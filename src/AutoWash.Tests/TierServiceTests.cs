using AutoWash.Application.DTOs;
using AutoWash.Application.Services;
using AutoWash.Domain.Entities;
using AutoWash.Domain.Enums;
using AutoWash.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AutoWash.Tests.Application.Services
{
  public class TierServiceTests
  {
    private static ApplicationDbContext CreateDbContext()
    {
      var options = new DbContextOptionsBuilder<ApplicationDbContext>()
          .UseInMemoryDatabase(Guid.NewGuid().ToString())
          .Options;

      return new ApplicationDbContext(options);
    }

    private static TierService CreateService(ApplicationDbContext dbContext) =>
        new TierService(dbContext, Mock.Of<ILogger<TierService>>());

    private static void SeedTiers(ApplicationDbContext dbContext)
    {
      dbContext.Tiers.Add(new Tier { TierID = 1, TierName = "Member", MinSpending = 0, BookingWindowDays = 7, DiscountRate = 0, PriorityScore = 1 });
      dbContext.Tiers.Add(new Tier { TierID = 2, TierName = "Silver", MinSpending = 1_000_000, BookingWindowDays = 10, DiscountRate = 5, PriorityScore = 2 });
      dbContext.Tiers.Add(new Tier { TierID = 3, TierName = "Gold", MinSpending = 3_000_000, BookingWindowDays = 12, DiscountRate = 10, PriorityScore = 3 });
      dbContext.SaveChanges();
    }

    private static Customer SeedCustomer(ApplicationDbContext dbContext, int tierId, decimal totalSpending)
    {
      var customer = new Customer
      {
        FullName = "Test User",
        Phone = "0901234567",
        Password = "hashed",
        TierID = tierId,
        TotalSpending = totalSpending,
        CreatedAt = DateTime.UtcNow
      };
      dbContext.Customers.Add(customer);
      dbContext.SaveChanges();
      return customer;
    }

    [Fact]
    public async Task GetAllTiersAsync_ShouldReturnTiersOrderedByPriorityScore()
    {
      using var dbContext = CreateDbContext();
      SeedTiers(dbContext);

      var service = CreateService(dbContext);
      var tiers = (await service.GetAllTiersAsync()).ToList();

      Assert.Equal(3, tiers.Count);
      Assert.Equal("Member", tiers[0].TierName);
      Assert.Equal("Silver", tiers[1].TierName);
      Assert.Equal("Gold", tiers[2].TierName);
    }

    [Fact]
    public async Task UpdateTierAsync_WithPartialFields_ShouldOnlyChangeProvidedFields()
    {
      using var dbContext = CreateDbContext();
      SeedTiers(dbContext);
      var service = CreateService(dbContext);

      var result = await service.UpdateTierAsync(2, new UpdateTierRequest { DiscountRate = 8m });

      Assert.Equal(8m, result.DiscountRate);
      Assert.Equal(1_000_000, result.MinSpending);
      Assert.Equal(10, result.BookingWindowDays);
    }

    [Fact]
    public async Task UpdateTierAsync_WithMinSpendingBelowLowerPriorityTier_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      SeedTiers(dbContext);
      var service = CreateService(dbContext);

      // Gold (PriorityScore=3) đang MinSpending=3,000,000; Silver (PriorityScore=2) đang 1,000,000.
      // Sửa Gold xuống 500,000 (thấp hơn Silver) sẽ đảo thứ tự PriorityScore <-> MinSpending.
      var ex = await Assert.ThrowsAsync<Exception>(() => service.UpdateTierAsync(3, new UpdateTierRequest { MinSpending = 500_000 }));

      Assert.StartsWith("INVALID_REQUEST", ex.Message);
      var persisted = await dbContext.Tiers.FirstAsync(t => t.TierID == 3);
      Assert.Equal(3_000_000, persisted.MinSpending); // không bị thay đổi khi request không hợp lệ
    }

    [Fact]
    public async Task UpdateTierAsync_WithMinSpendingKeepingOrder_ShouldSucceed()
    {
      using var dbContext = CreateDbContext();
      SeedTiers(dbContext);
      var service = CreateService(dbContext);

      var result = await service.UpdateTierAsync(3, new UpdateTierRequest { MinSpending = 4_000_000 });

      Assert.Equal(4_000_000, result.MinSpending);
    }

    [Fact]
    public async Task UpdateTierAsync_WithNonExistentTier_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      SeedTiers(dbContext);
      var service = CreateService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.UpdateTierAsync(999, new UpdateTierRequest { DiscountRate = 8m }));

      Assert.StartsWith("NOT_FOUND", ex.Message);
    }

    [Fact]
    public async Task EvaluateUpgradeAsync_WithSpendingMeetingHigherTier_ShouldUpgrade()
    {
      using var dbContext = CreateDbContext();
      SeedTiers(dbContext);
      var customer = SeedCustomer(dbContext, tierId: 1, totalSpending: 3_500_000);

      var service = CreateService(dbContext);
      await service.EvaluateUpgradeAsync(customer.CustomerID);

      var persisted = await dbContext.Customers.FirstAsync(c => c.CustomerID == customer.CustomerID);
      Assert.Equal(3, persisted.TierID); // Gold
    }

    [Fact]
    public async Task EvaluateUpgradeAsync_ShouldNeverDowngrade()
    {
      using var dbContext = CreateDbContext();
      SeedTiers(dbContext);
      // Khách đang ở Silver nhưng spending hiện tại chỉ đủ Member — EvaluateUpgrade chỉ dùng cho upgrade (BR-21)
      var customer = SeedCustomer(dbContext, tierId: 2, totalSpending: 0);

      var service = CreateService(dbContext);
      await service.EvaluateUpgradeAsync(customer.CustomerID);

      var persisted = await dbContext.Customers.FirstAsync(c => c.CustomerID == customer.CustomerID);
      Assert.Equal(2, persisted.TierID); // vẫn Silver, không bị hạ
    }

    [Fact]
    public async Task EvaluateUpgradeAsync_WithNonExistentCustomer_ShouldNotThrow()
    {
      using var dbContext = CreateDbContext();
      SeedTiers(dbContext);
      var service = CreateService(dbContext);

      var exception = await Record.ExceptionAsync(() => service.EvaluateUpgradeAsync(999));

      Assert.Null(exception);
    }

    [Fact]
    public async Task RunMonthlyDowngradeAsync_WithLowRecentSpending_ShouldDowngrade()
    {
      using var dbContext = CreateDbContext();
      SeedTiers(dbContext);
      var customer = SeedCustomer(dbContext, tierId: 2, totalSpending: 1_200_000);

      dbContext.Bookings.Add(new Booking
      {
        CustomerID = customer.CustomerID,
        Phone = customer.Phone,
        VehicleID = 1,
        LicensePlate = "51A-123.45",
        ServiceID = 1,
        ScheduledTime = DateTime.UtcNow.AddMonths(-1),
        Status = BookingStatus.Completed,
        FinalAmount = 200_000m, // dưới ngưỡng Silver (1,000,000) trong 12 tháng gần nhất
        CompletedAt = DateTime.UtcNow.AddMonths(-1)
      });
      await dbContext.SaveChangesAsync();

      var service = CreateService(dbContext);
      await service.RunMonthlyDowngradeAsync();

      var persisted = await dbContext.Customers.FirstAsync(c => c.CustomerID == customer.CustomerID);
      Assert.Equal(1, persisted.TierID); // hạ về Member
    }

    [Fact]
    public async Task RunMonthlyDowngradeAsync_ShouldNeverUpgrade()
    {
      using var dbContext = CreateDbContext();
      SeedTiers(dbContext);
      var customer = SeedCustomer(dbContext, tierId: 1, totalSpending: 3_500_000);

      dbContext.Bookings.Add(new Booking
      {
        CustomerID = customer.CustomerID,
        Phone = customer.Phone,
        VehicleID = 1,
        LicensePlate = "51A-123.45",
        ServiceID = 1,
        ScheduledTime = DateTime.UtcNow.AddMonths(-1),
        Status = BookingStatus.Completed,
        FinalAmount = 3_500_000m, // đủ điều kiện Gold trong 12 tháng gần nhất
        CompletedAt = DateTime.UtcNow.AddMonths(-1)
      });
      await dbContext.SaveChangesAsync();

      var service = CreateService(dbContext);
      await service.RunMonthlyDowngradeAsync();

      var persisted = await dbContext.Customers.FirstAsync(c => c.CustomerID == customer.CustomerID);
      Assert.Equal(1, persisted.TierID); // job downgrade không được phép nâng hạng
    }

    [Fact]
    public async Task RunMonthlyDowngradeAsync_ShouldIgnoreBookingsOlderThan12Months()
    {
      using var dbContext = CreateDbContext();
      SeedTiers(dbContext);
      var customer = SeedCustomer(dbContext, tierId: 2, totalSpending: 1_200_000);

      dbContext.Bookings.Add(new Booking
      {
        CustomerID = customer.CustomerID,
        Phone = customer.Phone,
        VehicleID = 1,
        LicensePlate = "51A-123.45",
        ServiceID = 1,
        ScheduledTime = DateTime.UtcNow.AddMonths(-13),
        Status = BookingStatus.Completed,
        FinalAmount = 5_000_000m, // đủ Gold nhưng đã quá 12 tháng, không được tính
        CompletedAt = DateTime.UtcNow.AddMonths(-13)
      });
      await dbContext.SaveChangesAsync();

      var service = CreateService(dbContext);
      await service.RunMonthlyDowngradeAsync();

      var persisted = await dbContext.Customers.FirstAsync(c => c.CustomerID == customer.CustomerID);
      Assert.Equal(1, persisted.TierID); // recentSpending = 0 => hạ về Member
    }

    [Fact]
    public async Task RunMonthlyDowngradeAsync_ShouldIgnoreNonCompletedBookings()
    {
      using var dbContext = CreateDbContext();
      SeedTiers(dbContext);
      var customer = SeedCustomer(dbContext, tierId: 2, totalSpending: 1_200_000);

      dbContext.Bookings.Add(new Booking
      {
        CustomerID = customer.CustomerID,
        Phone = customer.Phone,
        VehicleID = 1,
        LicensePlate = "51A-123.45",
        ServiceID = 1,
        ScheduledTime = DateTime.UtcNow,
        Status = BookingStatus.Cancelled,
        FinalAmount = 5_000_000m
      });
      await dbContext.SaveChangesAsync();

      var service = CreateService(dbContext);
      await service.RunMonthlyDowngradeAsync();

      var persisted = await dbContext.Customers.FirstAsync(c => c.CustomerID == customer.CustomerID);
      Assert.Equal(1, persisted.TierID); // booking Cancelled không tính, hạ về Member
    }
  }
}
