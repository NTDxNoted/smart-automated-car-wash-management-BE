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
  }
}
