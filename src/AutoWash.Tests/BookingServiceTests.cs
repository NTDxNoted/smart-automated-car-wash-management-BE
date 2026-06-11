using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;
using AutoWash.Application.Services;
using AutoWash.Domain.Entities;
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
    public async Task CreateBookingAsync_WithValidMemberBooking_ShouldCreatePendingBooking()
    {
      using var dbContext = CreateDbContext();
      dbContext.Tiers.Add(new Tier
      {
        TierID = 1,
        TierName = "Member",
        DiscountRate = 0,
        BookingWindowDays = 7,
        PriorityScore = 1
      });

      dbContext.Customers.Add(new Customer
      {
        CustomerID = 1,
        FullName = "Test Customer",
        Phone = "0901111001",
        Password = "pw",
        TierID = 1,
        Role = "MEMBER",
        IsLocked = false,
        CreatedAt = DateTime.UtcNow
      });

      dbContext.Vehicles.Add(new Vehicle
      {
        VehicleID = 1,
        CustomerID = 1,
        LicensePlate = "51A-001.11",
        IsActive = true
      });

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
        Phone = "0901111001",
        VehicleId = 1,
        ServiceId = 1,
        ScheduledTime = DateTime.UtcNow.AddHours(2),
        RewardId = null,
        PromoCode = null
      };

      var result = await service.CreateBookingAsync(request, 1);

      Assert.Equal("Pending", result.Status);
      Assert.Equal(80000m, result.Invoice.BaseAmount);
      Assert.Equal(1, await dbContext.Bookings.CountAsync());
    }
  }
}
