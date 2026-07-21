using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;
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
  public class BookingServiceTests
  {
    private static ApplicationDbContext CreateDbContext()
    {
      var options = new DbContextOptionsBuilder<ApplicationDbContext>()
          .UseInMemoryDatabase(Guid.NewGuid().ToString())
          .Options;

      return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateBookingAsync_WithMemberTier_ShouldReturnInvoiceAndServiceDetails()
    {
      using var dbContext = CreateDbContext();

      dbContext.Tiers.Add(new Tier
      {
        TierID = 2,
        TierName = "Silver",
        DiscountRate = 5,
        BookingWindowDays = 10,
        PriorityScore = 2
      });

      dbContext.Customers.Add(new Customer
      {
        CustomerID = 1,
        FullName = "Test Customer",
        Phone = "0901111111",
        Password = "pw",
        TierID = 2,
        Role = "MEMBER",
        IsLocked = false,
        CreatedAt = DateTime.UtcNow
      });

      dbContext.Vehicles.Add(new Vehicle
      {
        VehicleID = 1,
        CustomerID = 1,
        LicensePlate = "51A-000.11",
        IsActive = true
      });

      dbContext.Services.Add(new Service
      {
        ServiceID = 2,
        ServiceName = "Rửa xe cao cấp",
        ServiceCategory = "Premium",
        Price = 150000,
        Duration = 35,
        Status = "Active"
      });

      await dbContext.SaveChangesAsync();

      var logger = Mock.Of<ILogger<BookingService>>();
      var tierService = Mock.Of<ITierService>();
      var service = new BookingService(dbContext, logger, tierService);

      var request = new CreateBookingRequest
      {
        ServiceId = 2,
        VehicleId = 1,
        ScheduledTime = DateTime.UtcNow.AddHours(2),
        PromoCode = null,
        RewardId = null
      };

      var result = await service.CreateBookingAsync(request, 1);

      Assert.NotNull(result.Service);
      Assert.Equal(2, result.Service.ServiceId);
      Assert.Equal("Rửa xe cao cấp", result.Service.ServiceName);
      Assert.Equal(35, result.Service.Duration);
      Assert.NotNull(result.Invoice);
      Assert.Equal(150000m, result.Invoice.BaseAmount);
      Assert.Equal(7500m, result.Invoice.TierDiscount);
      Assert.Equal(0m, result.Invoice.RewardDiscount);
      Assert.Equal(0m, result.Invoice.PromotionDiscount);
      Assert.Equal(7500m, result.Invoice.DiscountApplied);
      Assert.Equal(142500m, result.Invoice.FinalAmount);
    }

    [Fact]
    public async Task CreateBookingAsync_WithGuestBooking_ShouldMapToDedicatedGuestCustomer()
    {
      using var dbContext = CreateDbContext();

      dbContext.Services.Add(new Service
      {
        ServiceID = 1,
        ServiceName = "Rửa xe cơ bản",
        ServiceCategory = "Basic",
        Price = 80000,
        Duration = 20,
        Status = "Active"
      });

      await dbContext.SaveChangesAsync();

      var logger = Mock.Of<ILogger<BookingService>>();
      var tierService = Mock.Of<ITierService>();
      var service = new BookingService(dbContext, logger, tierService);

      var request = new CreateBookingRequest
      {
        ServiceId = 1,
        Phone = "0909999888",
        LicensePlate = "51Z-999.88",
        ScheduledTime = DateTime.UtcNow.AddHours(2),
        PromoCode = null,
        RewardId = null
      };

      var result = await service.CreateBookingAsync(request, null);

      Assert.Equal("0909999888", result.Phone);
      Assert.True(result.BookingId > 0);
      Assert.True(await dbContext.Customers.AnyAsync(c => c.Phone == "GUEST" && c.FullName == "Khách vãng lai"));
      Assert.True(await dbContext.Bookings.AnyAsync(b => b.BookingID == result.BookingId && b.CustomerID > 0));
    }

    [Fact]
    public async Task CreateBookingAsync_WithMemberTier_ShouldApplyTierDiscount()
    {
      using var dbContext = CreateDbContext();

      dbContext.Tiers.Add(new Tier
      {
        TierID = 2,
        TierName = "Silver",
        DiscountRate = 5,
        BookingWindowDays = 10,
        PriorityScore = 2
      });

      dbContext.Customers.Add(new Customer
      {
        CustomerID = 1,
        FullName = "Test Customer",
        Phone = "0901111111",
        Password = "pw",
        TierID = 2,
        Role = "MEMBER",
        IsLocked = false,
        CreatedAt = DateTime.UtcNow
      });

      dbContext.Vehicles.Add(new Vehicle
      {
        VehicleID = 1,
        CustomerID = 1,
        LicensePlate = "51A-000.11",
        IsActive = true
      });

      dbContext.Services.Add(new Service
      {
        ServiceID = 1,
        ServiceName = "Rửa xe cơ bản",
        ServiceCategory = "Basic",
        Price = 100000,
        Duration = 20,
        Status = "Active"
      });

      await dbContext.SaveChangesAsync();

      var logger = Mock.Of<ILogger<BookingService>>();
      var tierService = Mock.Of<ITierService>();
      var service = new BookingService(dbContext, logger, tierService);

      var request = new CreateBookingRequest
      {
        ServiceId = 1,
        VehicleId = 1,
        ScheduledTime = DateTime.UtcNow.AddHours(2),
        Phone = null,
        LicensePlate = null,
        PromoCode = null,
        RewardId = null
      };

      var result = await service.CreateBookingAsync(request, 1);

      Assert.Equal(95000m, result.FinalAmount);
    }

    [Fact]
    public async Task CreateBookingAsync_WithCompletedBookingsSameDay_ShouldNotCountTowardDailyLimit()
    {
      using var dbContext = CreateDbContext();

      dbContext.Tiers.Add(new Tier
      {
        TierID = 2,
        TierName = "Silver",
        DiscountRate = 5,
        BookingWindowDays = 10,
        PriorityScore = 2
      });

      dbContext.Customers.Add(new Customer
      {
        CustomerID = 1,
        FullName = "Test Customer",
        Phone = "0901111111",
        Password = "pw",
        TierID = 2,
        Role = "MEMBER",
        IsLocked = false,
        CreatedAt = DateTime.UtcNow
      });

      dbContext.Vehicles.Add(new Vehicle
      {
        VehicleID = 1,
        CustomerID = 1,
        LicensePlate = "51A-000.11",
        IsActive = true
      });

      dbContext.Services.Add(new Service
      {
        ServiceID = 1,
        ServiceName = "Rửa xe cơ bản",
        ServiceCategory = "Basic",
        Price = 100000,
        Duration = 20,
        Status = "Active"
      });

      var today = DateTime.UtcNow.Date;
      dbContext.Bookings.AddRange(
          new Booking
          {
            BookingID = 1001,
            CustomerID = 1,
            ServiceID = 1,
            VehicleID = 1,
            ScheduledTime = today.AddHours(8),
            Status = BookingStatus.Completed,
            Phone = "0901111111",
            LicensePlate = "51A-000.11",
            FinalAmount = 100000m,
            CreatedAt = DateTime.UtcNow
          },
          new Booking
          {
            BookingID = 1002,
            CustomerID = 1,
            ServiceID = 1,
            VehicleID = 1,
            ScheduledTime = today.AddHours(10),
            Status = BookingStatus.Completed,
            Phone = "0901111111",
            LicensePlate = "51A-000.11",
            FinalAmount = 100000m,
            CreatedAt = DateTime.UtcNow
          });

      await dbContext.SaveChangesAsync();

      var logger = Mock.Of<ILogger<BookingService>>();
      var tierService = Mock.Of<ITierService>();
      var service = new BookingService(dbContext, logger, tierService);

      var request = new CreateBookingRequest
      {
        ServiceId = 1,
        VehicleId = 1,
        ScheduledTime = today.AddHours(16),
        Phone = null,
        LicensePlate = null,
        PromoCode = null,
        RewardId = null
      };

      var result = await service.CreateBookingAsync(request, 1);

      Assert.True(result.BookingId > 0);
      Assert.Equal("Pending", result.Status);
    }

    private static Booking SeedBookingForActions(ApplicationDbContext dbContext, BookingStatus status, DateTime scheduledTime, int customerId = 1, string phone = "0901111111", int pointsRedeemed = 0)
    {
      var booking = new Booking
      {
        CustomerID = customerId,
        Phone = phone,
        VehicleID = 1,
        LicensePlate = "51A-000.11",
        ServiceID = 1,
        ScheduledTime = scheduledTime,
        Status = status,
        FinalAmount = 100000m,
        PointsRedeemed = pointsRedeemed,
        CreatedAt = DateTime.UtcNow
      };
      dbContext.Bookings.Add(booking);
      dbContext.SaveChanges();
      return booking;
    }

    [Fact]
    public async Task GetBookingByIdAsync_AsOwner_ShouldReturnBooking()
    {
      using var dbContext = CreateDbContext();
      var booking = SeedBookingForActions(dbContext, BookingStatus.Pending, DateTime.UtcNow.AddHours(3), customerId: 1);

      var service = new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), Mock.Of<ITierService>());
      var result = await service.GetBookingByIdAsync(booking.BookingID, customerId: 1, guestPhone: null);

      Assert.Equal(booking.BookingID, result.BookingId);
    }

    [Fact]
    public async Task GetBookingByIdAsync_AsOtherCustomer_ShouldThrowUnauthorized()
    {
      using var dbContext = CreateDbContext();
      var booking = SeedBookingForActions(dbContext, BookingStatus.Pending, DateTime.UtcNow.AddHours(3), customerId: 1);

      var service = new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), Mock.Of<ITierService>());
      var ex = await Assert.ThrowsAsync<Exception>(() => service.GetBookingByIdAsync(booking.BookingID, customerId: 2, guestPhone: null));

      Assert.StartsWith("UNAUTHORIZED", ex.Message);
    }

    [Fact]
    public async Task GetBookingByIdAsync_AsGuestWithWrongPhone_ShouldThrowUnauthorized()
    {
      using var dbContext = CreateDbContext();
      var booking = SeedBookingForActions(dbContext, BookingStatus.Pending, DateTime.UtcNow.AddHours(3), customerId: 0, phone: "0901111111");

      var service = new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), Mock.Of<ITierService>());
      var ex = await Assert.ThrowsAsync<Exception>(() => service.GetBookingByIdAsync(booking.BookingID, customerId: null, guestPhone: "0999999999"));

      Assert.StartsWith("UNAUTHORIZED", ex.Message);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithNonExistentBooking_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var service = new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), Mock.Of<ITierService>());

      var ex = await Assert.ThrowsAsync<Exception>(() => service.GetBookingByIdAsync(999, customerId: 1, guestPhone: null));

      Assert.StartsWith("NOT_FOUND", ex.Message);
    }

    [Fact]
    public async Task CancelBookingAsync_MoreThan2HoursBefore_ShouldCancelAndRefundPoints()
    {
      using var dbContext = CreateDbContext();
      var customer = new Customer { CustomerID = 1, FullName = "Test", Phone = "0901111111", Password = "pw", CreatedAt = DateTime.UtcNow };
      dbContext.Customers.Add(customer);
      dbContext.LoyaltyAccounts.Add(new LoyaltyAccount { CustomerID = 1, TotalPoints = 10, LastUpdated = DateTime.UtcNow });
      await dbContext.SaveChangesAsync();
      var booking = SeedBookingForActions(dbContext, BookingStatus.Pending, DateTime.UtcNow.AddHours(3), customerId: 1, pointsRedeemed: 20);

      var service = new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), Mock.Of<ITierService>());
      var result = await service.CancelBookingAsync(booking.BookingID, customerId: 1, guestPhone: null);

      Assert.Equal("Cancelled", result.Status);
      Assert.Equal(20, result.PointsRefunded);

      var persistedLoyalty = await dbContext.LoyaltyAccounts.FirstAsync(l => l.CustomerID == 1);
      Assert.Equal(30, persistedLoyalty.TotalPoints);
    }

    [Fact]
    public async Task CancelBookingAsync_LessThan2HoursBefore_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var booking = SeedBookingForActions(dbContext, BookingStatus.Pending, DateTime.UtcNow.AddMinutes(90), customerId: 1);

      var service = new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), Mock.Of<ITierService>());
      var ex = await Assert.ThrowsAsync<Exception>(() => service.CancelBookingAsync(booking.BookingID, customerId: 1, guestPhone: null));

      Assert.StartsWith("CANCEL_TOO_LATE", ex.Message);
    }

    [Fact]
    public async Task CancelBookingAsync_WithNonPendingBooking_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var booking = SeedBookingForActions(dbContext, BookingStatus.Completed, DateTime.UtcNow.AddHours(3), customerId: 1);

      var service = new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), Mock.Of<ITierService>());
      var ex = await Assert.ThrowsAsync<Exception>(() => service.CancelBookingAsync(booking.BookingID, customerId: 1, guestPhone: null));

      Assert.StartsWith("INVALID_STATUS", ex.Message);
    }

    [Fact]
    public async Task CancelBookingAsync_ByWrongCustomer_ShouldThrowUnauthorized()
    {
      using var dbContext = CreateDbContext();
      var booking = SeedBookingForActions(dbContext, BookingStatus.Pending, DateTime.UtcNow.AddHours(3), customerId: 1);

      var service = new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), Mock.Of<ITierService>());
      var ex = await Assert.ThrowsAsync<Exception>(() => service.CancelBookingAsync(booking.BookingID, customerId: 2, guestPhone: null));

      Assert.StartsWith("UNAUTHORIZED", ex.Message);
    }

    [Fact]
    public async Task CompleteBookingAsync_ShouldMarkCompletedAndUpdateSpendingAndEvaluateTierUpgrade()
    {
      using var dbContext = CreateDbContext();
      var customer = new Customer { CustomerID = 1, FullName = "Test", Phone = "0901111111", Password = "pw", TotalSpending = 0m, CreatedAt = DateTime.UtcNow };
      dbContext.Customers.Add(customer);
      await dbContext.SaveChangesAsync();
      var booking = SeedBookingForActions(dbContext, BookingStatus.Pending, DateTime.UtcNow.AddHours(-1), customerId: 1);

      var tierServiceMock = new Mock<ITierService>();
      tierServiceMock.Setup(t => t.EvaluateUpgradeAsync(1)).Returns(Task.CompletedTask);
      var service = new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), tierServiceMock.Object);

      var result = await service.CompleteBookingAsync(booking.BookingID);

      Assert.Equal("Completed", result.Status);
      var persistedCustomer = await dbContext.Customers.FirstAsync(c => c.CustomerID == 1);
      Assert.Equal(100000m, persistedCustomer.TotalSpending);
      tierServiceMock.Verify(t => t.EvaluateUpgradeAsync(1), Times.Once);
    }

    [Fact]
    public async Task CompleteBookingAsync_WithNonPendingBooking_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var booking = SeedBookingForActions(dbContext, BookingStatus.Cancelled, DateTime.UtcNow.AddHours(-1), customerId: 1);

      var service = new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), Mock.Of<ITierService>());
      var ex = await Assert.ThrowsAsync<Exception>(() => service.CompleteBookingAsync(booking.BookingID));

      Assert.StartsWith("INVALID_STATUS", ex.Message);
    }

    [Fact]
    public async Task CompleteBookingAsync_WithNonExistentBooking_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var service = new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), Mock.Of<ITierService>());

      var ex = await Assert.ThrowsAsync<Exception>(() => service.CompleteBookingAsync(999));

      Assert.StartsWith("NOT_FOUND", ex.Message);
    }

    [Fact]
    public async Task GetCustomerBookingsAsync_ForCustomer_ShouldOnlyReturnTheirBookings()
    {
      using var dbContext = CreateDbContext();
      SeedBookingForActions(dbContext, BookingStatus.Pending, DateTime.UtcNow.AddHours(3), customerId: 1);
      SeedBookingForActions(dbContext, BookingStatus.Pending, DateTime.UtcNow.AddHours(3), customerId: 2);

      var service = new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), Mock.Of<ITierService>());
      var result = await service.GetCustomerBookingsAsync(customerId: 1, guestPhone: null, status: null, page: 1, pageSize: 10);

      Assert.Equal(1, result.Total);
    }

    [Fact]
    public async Task GetCustomerBookingsAsync_ForGuestPhone_ShouldMatchByPhone()
    {
      using var dbContext = CreateDbContext();
      SeedBookingForActions(dbContext, BookingStatus.Pending, DateTime.UtcNow.AddHours(3), customerId: 0, phone: "0909999999");

      var service = new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), Mock.Of<ITierService>());
      var result = await service.GetCustomerBookingsAsync(customerId: null, guestPhone: "0909999999", status: null, page: 1, pageSize: 10);

      Assert.Equal(1, result.Total);
    }

    [Fact]
    public async Task GetCustomerBookingsAsync_WithoutIdOrPhone_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var service = new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), Mock.Of<ITierService>());

      var ex = await Assert.ThrowsAsync<Exception>(() => service.GetCustomerBookingsAsync(customerId: null, guestPhone: null, status: null, page: 1, pageSize: 10));

      Assert.StartsWith("UNAUTHORIZED", ex.Message);
    }

    [Fact]
    public async Task GetCustomerBookingsAsync_WithStatusFilter_ShouldOnlyReturnMatching()
    {
      using var dbContext = CreateDbContext();
      SeedBookingForActions(dbContext, BookingStatus.Pending, DateTime.UtcNow.AddHours(3), customerId: 1);
      SeedBookingForActions(dbContext, BookingStatus.Completed, DateTime.UtcNow.AddHours(-3), customerId: 1);

      var service = new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), Mock.Of<ITierService>());
      var result = await service.GetCustomerBookingsAsync(customerId: 1, guestPhone: null, status: "Completed", page: 1, pageSize: 10);

      Assert.Equal(1, result.Total);
      Assert.Equal("Completed", result.Data[0].Status);
    }
  }
}
