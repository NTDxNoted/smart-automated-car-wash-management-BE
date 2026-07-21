using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;
using AutoWash.Application.Services;
using AutoWash.Domain.Entities;
using AutoWash.Domain.Enums;
using AutoWash.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

    private static BookingService CreateServiceWithPriorityBuffer(ApplicationDbContext dbContext, int maxParallelSlots, int priorityBufferSlots)
    {
      var options = Options.Create(new BookingSettings { MaxParallelSlots = maxParallelSlots, PriorityBufferSlots = priorityBufferSlots });
      return new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), Mock.Of<ITierService>(), options);
    }

    private static void SeedPriorityTiers(ApplicationDbContext dbContext)
    {
      dbContext.Tiers.Add(new Tier { TierID = 1, TierName = "Member", MinSpending = 0, BookingWindowDays = 7, DiscountRate = 0, PriorityScore = 1 });
      dbContext.Tiers.Add(new Tier { TierID = 2, TierName = "Silver", MinSpending = 1_000_000, BookingWindowDays = 10, DiscountRate = 5, PriorityScore = 2 });
      dbContext.SaveChanges();
    }

    private static Customer SeedCustomerWithVehicle(ApplicationDbContext dbContext, int customerId, int tierId, string phone, string plate)
    {
      var customer = new Customer { CustomerID = customerId, FullName = "Customer " + customerId, Phone = phone, Password = "pw", TierID = tierId, CreatedAt = DateTime.UtcNow };
      dbContext.Customers.Add(customer);
      dbContext.Vehicles.Add(new Vehicle { VehicleID = customerId, CustomerID = customerId, LicensePlate = plate, IsActive = true });
      dbContext.SaveChanges();
      return customer;
    }

    [Fact]
    public async Task CreateBookingAsync_WhenSlotFullAndCustomerIsBaseTier_ShouldThrowSlotNotAvailable()
    {
      using var dbContext = CreateDbContext();
      SeedPriorityTiers(dbContext);
      dbContext.Services.Add(new Service { ServiceID = 1, ServiceName = "Rửa cơ bản", ServiceCategory = "Basic", Price = 50000, Duration = 20, Status = "Active" });
      var occupant = SeedCustomerWithVehicle(dbContext, 1, tierId: 1, phone: "0901111111", plate: "51A-000.01");
      var requester = SeedCustomerWithVehicle(dbContext, 2, tierId: 1, phone: "0901111112", plate: "51A-000.02"); // cùng Member, không ưu tiên

      var scheduledTime = DateTime.UtcNow.AddHours(3);
      dbContext.Bookings.Add(new Booking { CustomerID = occupant.CustomerID, Phone = occupant.Phone, VehicleID = 1, LicensePlate = "51A-000.01", ServiceID = 1, ScheduledTime = scheduledTime, Status = BookingStatus.Pending, FinalAmount = 50000m, CreatedAt = DateTime.UtcNow });
      await dbContext.SaveChangesAsync();

      var service = CreateServiceWithPriorityBuffer(dbContext, maxParallelSlots: 1, priorityBufferSlots: 1);
      var request = new CreateBookingRequest { ServiceId = 1, VehicleId = 2, ScheduledTime = scheduledTime };

      var ex = await Assert.ThrowsAsync<Exception>(() => service.CreateBookingAsync(request, requester.CustomerID));

      Assert.StartsWith("SLOT_NOT_AVAILABLE", ex.Message);
    }

    [Fact]
    public async Task CreateBookingAsync_WhenSlotFullButCustomerIsHigherTier_ShouldSucceedUsingPriorityBuffer()
    {
      using var dbContext = CreateDbContext();
      SeedPriorityTiers(dbContext);
      dbContext.Services.Add(new Service { ServiceID = 1, ServiceName = "Rửa cơ bản", ServiceCategory = "Basic", Price = 50000, Duration = 20, Status = "Active" });
      var occupant = SeedCustomerWithVehicle(dbContext, 1, tierId: 1, phone: "0901111111", plate: "51A-000.01");
      var requester = SeedCustomerWithVehicle(dbContext, 2, tierId: 2, phone: "0901111112", plate: "51A-000.02"); // Silver > Member

      var scheduledTime = DateTime.UtcNow.AddHours(3);
      dbContext.Bookings.Add(new Booking { CustomerID = occupant.CustomerID, Phone = occupant.Phone, VehicleID = 1, LicensePlate = "51A-000.01", ServiceID = 1, ScheduledTime = scheduledTime, Status = BookingStatus.Pending, FinalAmount = 50000m, CreatedAt = DateTime.UtcNow });
      await dbContext.SaveChangesAsync();

      var service = CreateServiceWithPriorityBuffer(dbContext, maxParallelSlots: 1, priorityBufferSlots: 1);
      var request = new CreateBookingRequest { ServiceId = 1, VehicleId = 2, ScheduledTime = scheduledTime };

      var result = await service.CreateBookingAsync(request, requester.CustomerID);

      Assert.True(result.BookingId > 0);
      Assert.Equal("Pending", result.Status);
    }

    [Fact]
    public async Task CreateBookingAsync_WhenBaseSlotAndPriorityBufferBothFull_ShouldThrowEvenForHigherTier()
    {
      using var dbContext = CreateDbContext();
      SeedPriorityTiers(dbContext);
      dbContext.Services.Add(new Service { ServiceID = 1, ServiceName = "Rửa cơ bản", ServiceCategory = "Basic", Price = 50000, Duration = 20, Status = "Active" });
      var occupant1 = SeedCustomerWithVehicle(dbContext, 1, tierId: 1, phone: "0901111111", plate: "51A-000.01");
      var occupant2 = SeedCustomerWithVehicle(dbContext, 2, tierId: 2, phone: "0901111112", plate: "51A-000.02");
      var requester = SeedCustomerWithVehicle(dbContext, 3, tierId: 2, phone: "0901111113", plate: "51A-000.03");

      var scheduledTime = DateTime.UtcNow.AddHours(3);
      dbContext.Bookings.AddRange(
          new Booking { CustomerID = occupant1.CustomerID, Phone = occupant1.Phone, VehicleID = 1, LicensePlate = "51A-000.01", ServiceID = 1, ScheduledTime = scheduledTime, Status = BookingStatus.Pending, FinalAmount = 50000m, CreatedAt = DateTime.UtcNow },
          new Booking { CustomerID = occupant2.CustomerID, Phone = occupant2.Phone, VehicleID = 2, LicensePlate = "51A-000.02", ServiceID = 1, ScheduledTime = scheduledTime, Status = BookingStatus.Pending, FinalAmount = 50000m, CreatedAt = DateTime.UtcNow }
      );
      await dbContext.SaveChangesAsync();

      var service = CreateServiceWithPriorityBuffer(dbContext, maxParallelSlots: 1, priorityBufferSlots: 1);
      var request = new CreateBookingRequest { ServiceId = 1, VehicleId = 3, ScheduledTime = scheduledTime };

      var ex = await Assert.ThrowsAsync<Exception>(() => service.CreateBookingAsync(request, requester.CustomerID));

      Assert.StartsWith("SLOT_NOT_AVAILABLE", ex.Message);
    }

    [Fact]
    public async Task CreateBookingAsync_WithinBaseCapacity_ShouldNotNeedPriorityBuffer()
    {
      using var dbContext = CreateDbContext();
      SeedPriorityTiers(dbContext);
      dbContext.Services.Add(new Service { ServiceID = 1, ServiceName = "Rửa cơ bản", ServiceCategory = "Basic", Price = 50000, Duration = 20, Status = "Active" });
      var requester = SeedCustomerWithVehicle(dbContext, 1, tierId: 1, phone: "0901111111", plate: "51A-000.01");

      var scheduledTime = DateTime.UtcNow.AddHours(3);
      await dbContext.SaveChangesAsync();

      var service = CreateServiceWithPriorityBuffer(dbContext, maxParallelSlots: 2, priorityBufferSlots: 1);
      var request = new CreateBookingRequest { ServiceId = 1, VehicleId = 1, ScheduledTime = scheduledTime };

      var result = await service.CreateBookingAsync(request, requester.CustomerID);

      Assert.True(result.BookingId > 0);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldNotifyBookingHubWithAvailableStatus()
    {
      using var dbContext = CreateDbContext();
      dbContext.Services.Add(new Service { ServiceID = 1, ServiceName = "Rửa cơ bản", ServiceCategory = "Basic", Price = 50000, Duration = 20, Status = "Active" });
      await dbContext.SaveChangesAsync();

      var hubMock = new Mock<IBookingHubNotifier>();
      var options = Options.Create(new BookingSettings { MaxParallelSlots = 2, PriorityBufferSlots = 0 });
      var service = new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), Mock.Of<ITierService>(), null, options, null, hubMock.Object);

      var scheduledTime = DateTime.UtcNow.AddHours(3);
      var request = new CreateBookingRequest { ServiceId = 1, Phone = "0909999888", LicensePlate = "51Z-999.88", ScheduledTime = scheduledTime };

      await service.CreateBookingAsync(request, null);

      var expectedLocal = scheduledTime.AddHours(7);
      hubMock.Verify(h => h.NotifySlotOccupancyChangedAsync(
          expectedLocal.ToString("yyyy-MM-dd"),
          expectedLocal.ToString("HH:mm"),
          1, // maxParallelSlots(2) - occupied(1)
          "Available"), Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_WhenSlotBecomesFull_ShouldNotifyBookingHubWithFullStatus()
    {
      using var dbContext = CreateDbContext();
      dbContext.Services.Add(new Service { ServiceID = 1, ServiceName = "Rửa cơ bản", ServiceCategory = "Basic", Price = 50000, Duration = 20, Status = "Active" });
      await dbContext.SaveChangesAsync();

      var hubMock = new Mock<IBookingHubNotifier>();
      var options = Options.Create(new BookingSettings { MaxParallelSlots = 1, PriorityBufferSlots = 0 });
      var service = new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), Mock.Of<ITierService>(), null, options, null, hubMock.Object);

      var scheduledTime = DateTime.UtcNow.AddHours(3);
      var request = new CreateBookingRequest { ServiceId = 1, Phone = "0909999888", LicensePlate = "51Z-999.88", ScheduledTime = scheduledTime };

      await service.CreateBookingAsync(request, null);

      hubMock.Verify(h => h.NotifySlotOccupancyChangedAsync(
          It.IsAny<string>(), It.IsAny<string>(), 0, "Full"), Times.Once);
    }

    [Fact]
    public async Task CancelBookingAsync_ShouldNotifyBookingHubWithFreedSlot()
    {
      using var dbContext = CreateDbContext();
      dbContext.Services.Add(new Service { ServiceID = 1, ServiceName = "Rửa cơ bản", ServiceCategory = "Basic", Price = 50000, Duration = 20, Status = "Active" });
      await dbContext.SaveChangesAsync();

      var hubMock = new Mock<IBookingHubNotifier>();
      var options = Options.Create(new BookingSettings { MaxParallelSlots = 1, PriorityBufferSlots = 0 });
      var service = new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), Mock.Of<ITierService>(), null, options, null, hubMock.Object);

      var scheduledTime = DateTime.UtcNow.AddHours(3);
      var request = new CreateBookingRequest { ServiceId = 1, Phone = "0909999888", LicensePlate = "51Z-999.88", ScheduledTime = scheduledTime };
      var created = await service.CreateBookingAsync(request, null);

      await service.CancelBookingAsync(created.BookingId, null, "0909999888");

      hubMock.Verify(h => h.NotifySlotOccupancyChangedAsync(
          It.IsAny<string>(), It.IsAny<string>(), 1, "Available"), Times.Once);
    }
  }
}
