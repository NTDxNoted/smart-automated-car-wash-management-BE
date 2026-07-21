using AutoWash.Application.DTOs.Admin;
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
  // UpdateBookingStatusAsync (bao gồm nhánh phạt No-show) đã có test riêng trong
  // AdminCustomerAndBookingServiceTests.cs — file này cover 5 method còn lại của AdminBookingService.
  public class AdminBookingServiceTests
  {
    private static ApplicationDbContext CreateDbContext()
    {
      var options = new DbContextOptionsBuilder<ApplicationDbContext>()
          .UseInMemoryDatabase(Guid.NewGuid().ToString())
          .Options;

      return new ApplicationDbContext(options);
    }

    private static AdminBookingService CreateService(ApplicationDbContext dbContext) =>
        new AdminBookingService(dbContext, Mock.Of<ILogger<AdminBookingService>>());

    private static Booking SeedBooking(ApplicationDbContext dbContext, BookingStatus status, int customerId = 1, DateTime? checkInTime = null)
    {
      var booking = new Booking
      {
        CustomerID = customerId,
        Phone = "0901234567",
        VehicleID = 1,
        LicensePlate = "51A-123.45",
        ServiceID = 1,
        ScheduledTime = DateTime.UtcNow,
        Status = status,
        CheckInTime = checkInTime,
        FinalAmount = 100000m,
        CreatedAt = DateTime.UtcNow
      };
      dbContext.Bookings.Add(booking);
      dbContext.SaveChanges();
      return booking;
    }

    [Fact]
    public async Task GetAllBookingsAsync_ShouldIncludeCustomerAndServiceNames()
    {
      using var dbContext = CreateDbContext();
      dbContext.Customers.Add(new Customer { CustomerID = 1, FullName = "Test Customer", Phone = "0901234567", Password = "pw", CreatedAt = DateTime.UtcNow });
      dbContext.Services.Add(new Service { ServiceID = 1, ServiceName = "Rửa cơ bản", ServiceCategory = "Basic", Description = "d", Price = 50000m, Duration = 30 });
      await dbContext.SaveChangesAsync();
      SeedBooking(dbContext, BookingStatus.Pending, customerId: 1);

      var service = CreateService(dbContext);
      var result = (await service.GetAllBookingsAsync(null, null, null, null)).ToList();

      Assert.Single(result);
      Assert.Equal("Test Customer", result[0].CustomerName);
      Assert.Equal("Rửa cơ bản", result[0].ServiceName);
    }

    [Fact]
    public async Task GetAllBookingsAsync_ForGuestBooking_ShouldReturnGuestAndUnknownServiceFallback()
    {
      using var dbContext = CreateDbContext();
      SeedBooking(dbContext, BookingStatus.Pending, customerId: 999); // không có customer/service tương ứng

      var service = CreateService(dbContext);
      var result = (await service.GetAllBookingsAsync(null, null, null, null)).ToList();

      Assert.Equal("Guest", result[0].CustomerName);
      Assert.Equal("Unknown Service", result[0].ServiceName);
    }

    [Fact]
    public async Task GetAllBookingsAsync_WithStatusFilter_ShouldOnlyReturnMatching()
    {
      using var dbContext = CreateDbContext();
      SeedBooking(dbContext, BookingStatus.Pending);
      SeedBooking(dbContext, BookingStatus.Completed);

      var service = CreateService(dbContext);
      var result = (await service.GetAllBookingsAsync("Completed", null, null, null)).ToList();

      Assert.Single(result);
      Assert.Equal("Completed", result[0].Status);
    }

    [Fact]
    public async Task GetAllBookingsAsync_WithPhoneFilter_ShouldMatchPartialPhone()
    {
      using var dbContext = CreateDbContext();
      var booking = SeedBooking(dbContext, BookingStatus.Pending);

      var service = CreateService(dbContext);
      var result = (await service.GetAllBookingsAsync(null, null, "1234", null)).ToList();

      Assert.Single(result);
      Assert.Equal(booking.BookingID, result[0].BookingID);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithExistingBooking_ShouldReturnDetails()
    {
      using var dbContext = CreateDbContext();
      var booking = SeedBooking(dbContext, BookingStatus.Pending);

      var service = CreateService(dbContext);
      var result = await service.GetBookingByIdAsync(booking.BookingID);

      Assert.Equal(booking.BookingID, result.BookingID);
      Assert.Equal("Guest", result.CustomerName); // customerId=1 chưa seed Customer trong test này
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithNonExistentBooking_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var service = CreateService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.GetBookingByIdAsync(999));

      Assert.Equal("BOOKING_NOT_FOUND", ex.Message);
    }

    [Fact]
    public async Task UpdateLicensePlateAsync_WithPendingBooking_ShouldUpdatePlate()
    {
      using var dbContext = CreateDbContext();
      var booking = SeedBooking(dbContext, BookingStatus.Pending);

      var service = CreateService(dbContext);
      await service.UpdateLicensePlateAsync(booking.BookingID, new UpdateLicensePlateRequest { NewLicensePlate = "51A-999.99" });

      var persisted = await dbContext.Bookings.FirstAsync(b => b.BookingID == booking.BookingID);
      Assert.Equal("51A-999.99", persisted.LicensePlate);
    }

    [Fact]
    public async Task UpdateLicensePlateAsync_WithNonPendingBooking_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var booking = SeedBooking(dbContext, BookingStatus.Completed);

      var service = CreateService(dbContext);

      await Assert.ThrowsAsync<InvalidOperationException>(() =>
          service.UpdateLicensePlateAsync(booking.BookingID, new UpdateLicensePlateRequest { NewLicensePlate = "51A-999.99" }));
    }

    [Fact]
    public async Task UpdateLicensePlateAsync_WithNonExistentBooking_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var service = CreateService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() =>
          service.UpdateLicensePlateAsync(999, new UpdateLicensePlateRequest { NewLicensePlate = "51A-999.99" }));

      Assert.Equal("BOOKING_NOT_FOUND", ex.Message);
    }

    [Fact]
    public async Task CheckInAsync_WithPendingBooking_ShouldSetCheckInTime()
    {
      using var dbContext = CreateDbContext();
      var booking = SeedBooking(dbContext, BookingStatus.Pending);

      var service = CreateService(dbContext);
      await service.CheckInAsync(booking.BookingID);

      var persisted = await dbContext.Bookings.FirstAsync(b => b.BookingID == booking.BookingID);
      Assert.NotNull(persisted.CheckInTime);
    }

    [Fact]
    public async Task CheckInAsync_WithNonPendingBooking_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var booking = SeedBooking(dbContext, BookingStatus.Completed);

      var service = CreateService(dbContext);

      await Assert.ThrowsAsync<InvalidOperationException>(() => service.CheckInAsync(booking.BookingID));
    }

    [Fact]
    public async Task CheckInAsync_WithNonExistentBooking_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var service = CreateService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.CheckInAsync(999));

      Assert.Equal("BOOKING_NOT_FOUND", ex.Message);
    }

    [Fact]
    public async Task EmergencyStopAsync_WithCheckedInPendingBooking_ShouldMarkFailed()
    {
      using var dbContext = CreateDbContext();
      var booking = SeedBooking(dbContext, BookingStatus.Pending, checkInTime: DateTime.UtcNow.AddMinutes(-5));

      var service = CreateService(dbContext);
      await service.EmergencyStopAsync(booking.BookingID, new EmergencyStopRequest { Reason = "Máy hỏng" });

      var persisted = await dbContext.Bookings.FirstAsync(b => b.BookingID == booking.BookingID);
      Assert.Equal(BookingStatus.Failed, persisted.Status);
      Assert.NotNull(persisted.CompletedAt);
    }

    [Fact]
    public async Task EmergencyStopAsync_WithoutCheckIn_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var booking = SeedBooking(dbContext, BookingStatus.Pending, checkInTime: null);

      var service = CreateService(dbContext);

      var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
          service.EmergencyStopAsync(booking.BookingID, new EmergencyStopRequest { Reason = "x" }));

      Assert.StartsWith("BOOKING_NOT_STARTED", ex.Message);
    }

    [Fact]
    public async Task EmergencyStopAsync_WithAlreadyFinishedBooking_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var booking = SeedBooking(dbContext, BookingStatus.Completed, checkInTime: DateTime.UtcNow.AddMinutes(-5));

      var service = CreateService(dbContext);

      var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
          service.EmergencyStopAsync(booking.BookingID, new EmergencyStopRequest { Reason = "x" }));

      Assert.StartsWith("INVALID_STATUS", ex.Message);
    }

    [Fact]
    public async Task EmergencyStopAsync_WithNonExistentBooking_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var service = CreateService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() =>
          service.EmergencyStopAsync(999, new EmergencyStopRequest { Reason = "x" }));

      Assert.Equal("BOOKING_NOT_FOUND", ex.Message);
    }
  }
}
