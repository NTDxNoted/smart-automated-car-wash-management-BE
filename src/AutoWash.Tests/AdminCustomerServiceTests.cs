using AutoWash.Application.Services;
using AutoWash.Domain.Entities;
using AutoWash.Domain.Enums;
using AutoWash.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AutoWash.Tests.Application.Services
{
  // GetCustomersAsync có 1 test cơ bản trong AdminCustomerAndBookingServiceTests.cs —
  // file này cover 2 method chưa có test (GetCustomerByIdAsync, ToggleLockCustomerAsync)
  // và nhánh filter theo tier/isLocked của GetCustomersAsync.
  public class AdminCustomerServiceTests
  {
    private static ApplicationDbContext CreateDbContext()
    {
      var options = new DbContextOptionsBuilder<ApplicationDbContext>()
          .UseInMemoryDatabase(Guid.NewGuid().ToString())
          .Options;

      return new ApplicationDbContext(options);
    }

    private static Customer SeedCustomer(ApplicationDbContext dbContext, int points, bool isLocked = false)
    {
      var customer = new Customer
      {
        FullName = "Test Customer",
        Phone = "0901234567",
        Password = "pw",
        IsLocked = isLocked,
        CreatedAt = DateTime.UtcNow,
        LoyaltyAccount = new LoyaltyAccount { TotalPoints = points, LastUpdated = DateTime.UtcNow }
      };
      dbContext.Customers.Add(customer);
      dbContext.SaveChanges();
      return customer;
    }

    [Fact]
    public async Task GetCustomerByIdAsync_ShouldReturnDetailWithBookingHistoryAndServiceNames()
    {
      using var dbContext = CreateDbContext();
      var customer = SeedCustomer(dbContext, points: 100);
      dbContext.Services.Add(new Service { ServiceID = 1, ServiceName = "Rửa cơ bản", ServiceCategory = "Basic", Description = "d", Price = 50000m, Duration = 30 });
      dbContext.Bookings.Add(new Booking
      {
        CustomerID = customer.CustomerID,
        Phone = customer.Phone,
        VehicleID = 1,
        LicensePlate = "51A-123.45",
        ServiceID = 1,
        ScheduledTime = DateTime.UtcNow,
        Status = BookingStatus.Completed,
        FinalAmount = 50000m,
        PointsEarned = 5,
        CreatedAt = DateTime.UtcNow
      });
      await dbContext.SaveChangesAsync();

      var service = new AdminCustomerService(dbContext);
      var result = await service.GetCustomerByIdAsync(customer.CustomerID);

      Assert.Equal(customer.CustomerID, result.CustomerId);
      Assert.Single(result.BookingHistory);
      Assert.Equal("Rửa cơ bản", result.BookingHistory[0].Service.ServiceName);
    }

    [Fact]
    public async Task GetCustomerByIdAsync_WithNonExistentCustomer_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var service = new AdminCustomerService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.GetCustomerByIdAsync(999));

      Assert.StartsWith("NOT_FOUND", ex.Message);
    }

    [Fact]
    public async Task GetCustomerByIdAsync_WithoutLoyaltyAccount_ShouldDefaultToMemberTier()
    {
      using var dbContext = CreateDbContext();
      var customer = new Customer { FullName = "No Loyalty", Phone = "0909999999", Password = "pw", CreatedAt = DateTime.UtcNow };
      dbContext.Customers.Add(customer);
      await dbContext.SaveChangesAsync();

      var service = new AdminCustomerService(dbContext);
      var result = await service.GetCustomerByIdAsync(customer.CustomerID);

      Assert.Equal("Member", result.Tier);
      Assert.Equal(0, result.Points);
    }

    [Fact]
    public async Task ToggleLockCustomerAsync_ShouldLockAnUnlockedCustomer()
    {
      using var dbContext = CreateDbContext();
      var customer = SeedCustomer(dbContext, points: 0, isLocked: false);

      var service = new AdminCustomerService(dbContext);
      var result = await service.ToggleLockCustomerAsync(customer.CustomerID);

      Assert.True(result.IsLocked);
      var persisted = await dbContext.Customers.FirstAsync(c => c.CustomerID == customer.CustomerID);
      Assert.True(persisted.IsLocked);
    }

    [Fact]
    public async Task ToggleLockCustomerAsync_ShouldUnlockALockedCustomer()
    {
      using var dbContext = CreateDbContext();
      var customer = SeedCustomer(dbContext, points: 0, isLocked: true);

      var service = new AdminCustomerService(dbContext);
      var result = await service.ToggleLockCustomerAsync(customer.CustomerID);

      Assert.False(result.IsLocked);
    }

    [Fact]
    public async Task ToggleLockCustomerAsync_WithNonExistentCustomer_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var service = new AdminCustomerService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.ToggleLockCustomerAsync(999));

      Assert.StartsWith("NOT_FOUND", ex.Message);
    }

    [Theory]
    [InlineData(6000, "platinum")]
    [InlineData(2500, "gold")]
    [InlineData(800, "silver")]
    [InlineData(100, "member")]
    public async Task GetCustomersAsync_WithTierFilter_ShouldOnlyReturnMatchingTier(int points, string tierFilter)
    {
      using var dbContext = CreateDbContext();
      SeedCustomer(dbContext, points: 6000); // Platinum
      SeedCustomer(dbContext, points: 2500); // Gold
      SeedCustomer(dbContext, points: 800);  // Silver
      SeedCustomer(dbContext, points: 100);  // Member

      var service = new AdminCustomerService(dbContext);
      var result = await service.GetCustomersAsync(tierFilter, null, 1, 10);

      Assert.Single(result.Data);
      Assert.Equal(points, result.Data[0].Points);
    }

    [Fact]
    public async Task GetCustomersAsync_WithIsLockedFilter_ShouldOnlyReturnMatching()
    {
      using var dbContext = CreateDbContext();
      SeedCustomer(dbContext, points: 0, isLocked: true);
      SeedCustomer(dbContext, points: 0, isLocked: false);

      var service = new AdminCustomerService(dbContext);
      var result = await service.GetCustomersAsync(null, true, 1, 10);

      Assert.Single(result.Data);
      Assert.True(result.Data[0].IsLocked);
    }

    [Fact]
    public async Task GetCustomersAsync_ShouldRespectPagination()
    {
      using var dbContext = CreateDbContext();
      for (int i = 0; i < 5; i++)
      {
        SeedCustomer(dbContext, points: 0);
      }

      var service = new AdminCustomerService(dbContext);
      var page1 = await service.GetCustomersAsync(null, null, 1, 2);
      var page2 = await service.GetCustomersAsync(null, null, 2, 2);

      Assert.Equal(5, page1.Total);
      Assert.Equal(2, page1.Data.Count);
      Assert.Equal(2, page2.Data.Count);
    }
  }
}
