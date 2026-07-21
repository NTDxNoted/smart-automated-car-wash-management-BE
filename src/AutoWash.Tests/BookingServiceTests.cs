using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;
using AutoWash.Application.Services;
using AutoWash.Domain.Entities;
using AutoWash.Domain.Enums;
using AutoWash.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
      // CreateBookingAsync dùng Database.BeginTransactionAsync() để khóa advisory chống race điều
      // kiện slot; in-memory provider không hỗ trợ transaction thật nên phải ignore warning này.
      var options = new DbContextOptionsBuilder<ApplicationDbContext>()
          .UseInMemoryDatabase(Guid.NewGuid().ToString())
          .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
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
    public async Task GetAvailableSlotsAsync_ShouldMarkTheActuallyBookedLocalSlotAsUnavailable()
    {
      using var dbContext = CreateDbContext();
      dbContext.Services.Add(new Service { ServiceID = 1, ServiceName = "Rửa cơ bản", ServiceCategory = "Basic", Price = 50000, Duration = 20, Status = "Active" });
      await dbContext.SaveChangesAsync();

      // Chọn ngày mai (giờ VN) để chắc chắn nằm trong window hiển thị mặc định (7 ngày) và
      // tránh trường hợp biên nửa đêm ảnh hưởng tới BR-29 (advance notice 60 phút).
      // Các slot được sinh ra lúc :30 (07:30, 08:30, ...) nên đặt trùng giờ :30 để chắc chắn overlap.
      var targetLocalDate = DateTime.UtcNow.AddHours(7).Date.AddDays(1);
      var bookedLocalTime = targetLocalDate.AddHours(14).AddMinutes(30); // 14:30 giờ VN
      var bookedUtc = bookedLocalTime.AddHours(-7); // ScheduledTime phải lưu dưới dạng UTC

      dbContext.Bookings.Add(new Booking
      {
        CustomerID = 0,
        Phone = "0909999888",
        VehicleID = 0,
        LicensePlate = "51Z-999.88",
        ServiceID = 1,
        ScheduledTime = bookedUtc,
        Status = BookingStatus.Pending,
        FinalAmount = 50000m,
        CreatedAt = DateTime.UtcNow
      });
      await dbContext.SaveChangesAsync();

      var service = new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), Mock.Of<ITierService>());
      var slots = (await service.GetAvailableSlotsAsync(null, targetLocalDate.ToString("yyyy-MM-dd"), null)).ToList();

      var daySlots = Assert.Single(slots).Slots;
      var bookedSlot = daySlots.Single(s => s.Time == "14:30");

      Assert.False(bookedSlot.IsAvailable);
      Assert.Equal(0, bookedSlot.AvailableCount);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_WithSameLicensePlateWithin120Minutes_ShouldBeUnavailable()
    {
      using var dbContext = CreateDbContext();
      dbContext.Services.Add(new Service { ServiceID = 1, ServiceName = "Rửa cơ bản", ServiceCategory = "Basic", Price = 50000, Duration = 20, Status = "Active" });
      await dbContext.SaveChangesAsync();

      var targetLocalDate = DateTime.UtcNow.AddHours(7).Date.AddDays(1);
      var bookedLocalTime = targetLocalDate.AddHours(14).AddMinutes(30); // 14:30 giờ VN
      var bookedUtc = bookedLocalTime.AddHours(-7);

      dbContext.Bookings.Add(new Booking
      {
        CustomerID = 0,
        Phone = "0909999888",
        VehicleID = 0,
        LicensePlate = "51Z-999.88",
        ServiceID = 1,
        ScheduledTime = bookedUtc,
        Status = BookingStatus.Pending,
        FinalAmount = 50000m,
        CreatedAt = DateTime.UtcNow
      });
      await dbContext.SaveChangesAsync();

      var service = new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), Mock.Of<ITierService>());
      // Slot 15:30 giờ VN cách slot đã đặt (14:30) đúng 60 phút — trong vùng đệm BR-28 (<120 phút)
      var slots = (await service.GetAvailableSlotsAsync(null, targetLocalDate.ToString("yyyy-MM-dd"), "51Z-999.88")).ToList();

      var daySlots = Assert.Single(slots).Slots;
      var nearbySlot = daySlots.Single(s => s.Time == "15:30");

      Assert.False(nearbySlot.IsAvailable);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_WithDifferentLicensePlateWithin120Minutes_ShouldStillBeAvailable()
    {
      using var dbContext = CreateDbContext();
      dbContext.Services.Add(new Service { ServiceID = 1, ServiceName = "Rửa cơ bản", ServiceCategory = "Basic", Price = 50000, Duration = 20, Status = "Active" });
      await dbContext.SaveChangesAsync();

      var targetLocalDate = DateTime.UtcNow.AddHours(7).Date.AddDays(1);
      var bookedLocalTime = targetLocalDate.AddHours(14).AddMinutes(30);
      var bookedUtc = bookedLocalTime.AddHours(-7);

      dbContext.Bookings.Add(new Booking
      {
        CustomerID = 0,
        Phone = "0909999888",
        VehicleID = 0,
        LicensePlate = "51Z-999.88",
        ServiceID = 1,
        ScheduledTime = bookedUtc,
        Status = BookingStatus.Pending,
        FinalAmount = 50000m,
        CreatedAt = DateTime.UtcNow
      });
      await dbContext.SaveChangesAsync();

      var service = new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), Mock.Of<ITierService>());
      // Biển số khác — không bị ràng buộc BR-28, chỉ ràng buộc bởi overlap thời gian (không trùng slot 14:30)
      var slots = (await service.GetAvailableSlotsAsync(null, targetLocalDate.ToString("yyyy-MM-dd"), "29A-11111")).ToList();

      var daySlots = Assert.Single(slots).Slots;
      var nearbySlot = daySlots.Single(s => s.Time == "15:30");

      Assert.True(nearbySlot.IsAvailable);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_ShouldRespectTierBookingWindowDays()
    {
      using var dbContext = CreateDbContext();
      dbContext.Tiers.Add(new Tier { TierID = 2, TierName = "Silver", MinSpending = 0, BookingWindowDays = 2, DiscountRate = 5, PriorityScore = 2 });
      var customer = new Customer { CustomerID = 1, FullName = "Test", Phone = "0901111111", Password = "pw", TierID = 2, CreatedAt = DateTime.UtcNow };
      dbContext.Customers.Add(customer);
      await dbContext.SaveChangesAsync();

      var service = new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), Mock.Of<ITierService>());
      var slots = (await service.GetAvailableSlotsAsync(customer.CustomerID, null, null)).ToList();

      Assert.Equal(2, slots.Count); // BookingWindowDays = 2, không phải mặc định 7 của Guest
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_WhenSlotFullButCustomerIsHigherTier_ShouldStillShowAvailableViaPriorityBuffer()
    {
      using var dbContext = CreateDbContext();
      SeedPriorityTiers(dbContext);
      dbContext.Services.Add(new Service { ServiceID = 1, ServiceName = "Rửa cơ bản", ServiceCategory = "Basic", Price = 50000, Duration = 20, Status = "Active" });
      var occupant = SeedCustomerWithVehicle(dbContext, 1, tierId: 1, phone: "0901111111", plate: "51A-000.01");
      var requester = SeedCustomerWithVehicle(dbContext, 2, tierId: 2, phone: "0901111112", plate: "51A-000.02"); // Silver > Member

      var targetLocalDate = DateTime.UtcNow.AddHours(7).Date.AddDays(1);
      var bookedLocalTime = targetLocalDate.AddHours(14).AddMinutes(30); // 14:30 giờ VN, khớp lưới slot :30
      var bookedUtc = bookedLocalTime.AddHours(-7);

      dbContext.Bookings.Add(new Booking
      {
        CustomerID = occupant.CustomerID,
        Phone = occupant.Phone,
        VehicleID = 1,
        LicensePlate = "51A-000.01",
        ServiceID = 1,
        ScheduledTime = bookedUtc,
        Status = BookingStatus.Pending,
        FinalAmount = 50000m,
        CreatedAt = DateTime.UtcNow
      });
      await dbContext.SaveChangesAsync();

      var service = CreateServiceWithPriorityBuffer(dbContext, maxParallelSlots: 1, priorityBufferSlots: 1);

      // Khách Member thường (không ưu tiên) thấy slot 14:30 đã đầy — khớp với CreateBookingAsync.
      var baseTierSlots = (await service.GetAvailableSlotsAsync(occupant.CustomerID, targetLocalDate.ToString("yyyy-MM-dd"), null)).ToList();
      var baseTierSlot = Assert.Single(baseTierSlots).Slots.Single(s => s.Time == "14:30");
      Assert.False(baseTierSlot.IsAvailable);

      // Khách Silver (ưu tiên) vẫn được tính vào buffer ưu tiên nên slot phải hiện available,
      // khớp với việc CreateBookingAsync thực sự cho phép khách này đặt slot đó.
      var higherTierSlots = (await service.GetAvailableSlotsAsync(requester.CustomerID, targetLocalDate.ToString("yyyy-MM-dd"), null)).ToList();
      var higherTierSlot = Assert.Single(higherTierSlots).Slots.Single(s => s.Time == "14:30");
      Assert.True(higherTierSlot.IsAvailable);
      Assert.Equal(1, higherTierSlot.AvailableCount);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_WithDateFilter_ShouldOnlyReturnThatDate()
    {
      using var dbContext = CreateDbContext();
      var service = new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), Mock.Of<ITierService>());

      var targetLocalDate = DateTime.UtcNow.AddHours(7).Date.AddDays(1);
      var slots = (await service.GetAvailableSlotsAsync(null, targetLocalDate.ToString("yyyy-MM-dd"), null)).ToList();

      Assert.Single(slots);
      Assert.Equal(targetLocalDate.ToString("yyyy-MM-dd"), slots[0].Date);
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

    [Fact]
    public async Task CreateBookingAsync_AsGuestWithInvalidLicensePlate_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      dbContext.Services.Add(new Service { ServiceID = 1, ServiceName = "Rửa xe cơ bản", ServiceCategory = "Basic", Price = 80000, Duration = 20, Status = "Active" });
      await dbContext.SaveChangesAsync();

      var service = new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), Mock.Of<ITierService>());
      var request = new CreateBookingRequest
      {
        ServiceId = 1,
        Phone = "0909999888",
        LicensePlate = "AEDADAWDAWD",
        ScheduledTime = DateTime.UtcNow.AddHours(2)
      };

      var ex = await Assert.ThrowsAsync<Exception>(() => service.CreateBookingAsync(request, null));

      Assert.StartsWith("INVALID_LICENSE_PLATE", ex.Message);
    }

    [Fact]
    public async Task CreateBookingAsync_AsMemberWithInvalidInlineLicensePlate_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      SeedPriorityTiers(dbContext);
      dbContext.Services.Add(new Service { ServiceID = 1, ServiceName = "Rửa xe cơ bản", ServiceCategory = "Basic", Price = 80000, Duration = 20, Status = "Active" });
      var customer = SeedCustomerWithVehicle(dbContext, 1, tierId: 1, phone: "0901111111", plate: "51A-000.01");

      var service = new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), Mock.Of<ITierService>());
      var request = new CreateBookingRequest
      {
        ServiceId = 1,
        LicensePlate = "12345", // không qua VehicleId, nhập tay biển số mới sai định dạng
        ScheduledTime = DateTime.UtcNow.AddHours(2)
      };

      var ex = await Assert.ThrowsAsync<Exception>(() => service.CreateBookingAsync(request, customer.CustomerID));

      Assert.StartsWith("INVALID_LICENSE_PLATE", ex.Message);
    }

    private static (Customer customer, RewardsCatalog reward) SeedCustomerWithRewardEligibility(
        ApplicationDbContext dbContext, string discountType, decimal discountAmount, int loyaltyPoints = 100)
    {
      dbContext.Tiers.Add(new Tier { TierID = 1, TierName = "Member", MinSpending = 0, BookingWindowDays = 7, DiscountRate = 0, PriorityScore = 1 });
      var customer = new Customer { CustomerID = 1, FullName = "Reward Customer", Phone = "0901111111", Password = "pw", TierID = 1, CreatedAt = DateTime.UtcNow };
      dbContext.Customers.Add(customer);
      dbContext.Vehicles.Add(new Vehicle { VehicleID = 1, CustomerID = 1, LicensePlate = "51A-000.01", IsActive = true });
      dbContext.LoyaltyAccounts.Add(new LoyaltyAccount { CustomerID = 1, TotalPoints = loyaltyPoints, LastUpdated = DateTime.UtcNow });
      var reward = new RewardsCatalog { RewardID = 1, RewardName = "Reward", Description = "d", PointsRequired = 50, DiscountAmount = discountAmount, DiscountType = discountType, IsActive = true };
      dbContext.RewardsCatalog.Add(reward);
      dbContext.Services.Add(new Service { ServiceID = 1, ServiceName = "Rửa cơ bản", ServiceCategory = "Basic", Price = 100000m, Duration = 20, Status = "Active" });
      dbContext.SaveChanges();
      return (customer, reward);
    }

    [Fact]
    public async Task CreateBookingAsync_WithPercentageReward_ShouldApplyPercentageOfBaseAmount()
    {
      using var dbContext = CreateDbContext();
      var (customer, reward) = SeedCustomerWithRewardEligibility(dbContext, "Percentage", 10m);

      var service = new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), Mock.Of<ITierService>());
      var request = new CreateBookingRequest { ServiceId = 1, VehicleId = 1, ScheduledTime = DateTime.UtcNow.AddHours(2), RewardId = reward.RewardID };

      var result = await service.CreateBookingAsync(request, customer.CustomerID);

      Assert.Equal(10000m, result.Invoice.RewardDiscount); // 10% của 100,000
      Assert.Equal(90000m, result.Invoice.FinalAmount);
    }

    [Fact]
    public async Task CreateBookingAsync_WithFixedAmountReward_ShouldApplyFixedDiscount()
    {
      using var dbContext = CreateDbContext();
      var (customer, reward) = SeedCustomerWithRewardEligibility(dbContext, "Fixed_Amount", 15000m);

      var service = new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), Mock.Of<ITierService>());
      var request = new CreateBookingRequest { ServiceId = 1, VehicleId = 1, ScheduledTime = DateTime.UtcNow.AddHours(2), RewardId = reward.RewardID };

      var result = await service.CreateBookingAsync(request, customer.CustomerID);

      Assert.Equal(15000m, result.Invoice.RewardDiscount);
      Assert.Equal(85000m, result.Invoice.FinalAmount);
    }

    [Fact]
    public async Task CreateBookingAsync_WithRewardExceeding50PercentOfBaseAmount_ShouldCapAt50Percent()
    {
      using var dbContext = CreateDbContext();
      // Percentage 80% của 100,000 = 80,000, vượt trần BR-60 (tối đa 50% hóa đơn = 50,000)
      var (customer, reward) = SeedCustomerWithRewardEligibility(dbContext, "Percentage", 80m);

      var service = new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), Mock.Of<ITierService>());
      var request = new CreateBookingRequest { ServiceId = 1, VehicleId = 1, ScheduledTime = DateTime.UtcNow.AddHours(2), RewardId = reward.RewardID };

      var result = await service.CreateBookingAsync(request, customer.CustomerID);

      Assert.Equal(50000m, result.Invoice.RewardDiscount);
    }

    [Fact]
    public async Task CreateBookingAsync_WithInsufficientPointsForReward_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var (customer, reward) = SeedCustomerWithRewardEligibility(dbContext, "Fixed_Amount", 15000m, loyaltyPoints: 10); // < 50 điểm yêu cầu

      var service = new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), Mock.Of<ITierService>());
      var request = new CreateBookingRequest { ServiceId = 1, VehicleId = 1, ScheduledTime = DateTime.UtcNow.AddHours(2), RewardId = reward.RewardID };

      var ex = await Assert.ThrowsAsync<Exception>(() => service.CreateBookingAsync(request, customer.CustomerID));

      Assert.StartsWith("INSUFFICIENT_POINTS", ex.Message);
    }

    [Fact]
    public async Task CreateBookingAsync_WithNonExistentReward_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var (customer, _) = SeedCustomerWithRewardEligibility(dbContext, "Fixed_Amount", 15000m);

      var service = new BookingService(dbContext, Mock.Of<ILogger<BookingService>>(), Mock.Of<ITierService>());
      var request = new CreateBookingRequest { ServiceId = 1, VehicleId = 1, ScheduledTime = DateTime.UtcNow.AddHours(2), RewardId = 999 };

      var ex = await Assert.ThrowsAsync<Exception>(() => service.CreateBookingAsync(request, customer.CustomerID));

      Assert.StartsWith("REWARD_NOT_FOUND", ex.Message);
    }
  }
}
