using AutoWash.Application.DTOs;
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
  public class AdminCustomerAndBookingServiceTests
  {
    private static ApplicationDbContext CreateDbContext()
    {
      var options = new DbContextOptionsBuilder<ApplicationDbContext>()
          .UseInMemoryDatabase(Guid.NewGuid().ToString())
          .Options;

      return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetCustomersAsync_ShouldReturnPointsFromLoyaltyAccount()
    {
      using var dbContext = CreateDbContext();

      dbContext.Customers.Add(new Customer
      {
        CustomerID = 1,
        FullName = "Test Customer",
        Phone = "0901111111",
        Password = "pw",
        CreatedAt = DateTime.UtcNow,
        LoyaltyAccount = new LoyaltyAccount
        {
          CustomerID = 1,
          TotalPoints = 1250,
          LastUpdated = DateTime.UtcNow
        }
      });

      await dbContext.SaveChangesAsync();

      var service = new AdminCustomerService(dbContext);

      var result = await service.GetCustomersAsync(null, null, 1, 10);

      Assert.Single(result.Data);
      Assert.Equal(1250, result.Data[0].Points);
    }

    [Fact]
    public async Task UpdateBookingStatusAsync_WhenNoShowThresholdReached_ShouldSuspendCustomer()
    {
      using var dbContext = CreateDbContext();

      var customer = new Customer
      {
        CustomerID = 1,
        FullName = "Test Customer",
        Phone = "0902222222",
        Password = "pw",
        CreatedAt = DateTime.UtcNow
      };
      dbContext.Customers.Add(customer);

      dbContext.Bookings.AddRange(
          new Booking
          {
            BookingID = 1,
            CustomerID = 1,
            Phone = "0902222222",
            ServiceID = 1,
            LicensePlate = "51A-111.11",
            ScheduledTime = DateTime.UtcNow.AddMinutes(-20),
            Status = BookingStatus.NoShow,
            CompletedAt = DateTime.UtcNow.AddDays(-20),
            CreatedAt = DateTime.UtcNow.AddDays(-20)
          },
          new Booking
          {
            BookingID = 2,
            CustomerID = 1,
            Phone = "0902222222",
            ServiceID = 1,
            LicensePlate = "51A-111.11",
            ScheduledTime = DateTime.UtcNow.AddMinutes(-10),
            Status = BookingStatus.NoShow,
            CompletedAt = DateTime.UtcNow.AddDays(-10),
            CreatedAt = DateTime.UtcNow.AddDays(-10)
          }
      );

      dbContext.Bookings.Add(new Booking
      {
        BookingID = 3,
        CustomerID = 1,
        Phone = "0902222222",
        ServiceID = 1,
        LicensePlate = "51A-111.11",
        ScheduledTime = DateTime.UtcNow.AddMinutes(10),
        Status = BookingStatus.Pending,
        CreatedAt = DateTime.UtcNow
      });

      await dbContext.SaveChangesAsync();

      var logger = Mock.Of<ILogger<AdminBookingService>>();
      var service = new AdminBookingService(dbContext, logger);

      var result = await service.UpdateBookingStatusAsync(3, new UpdateBookingStatusRequest
      {
        NewStatus = "NoShow"
      });

      Assert.NotNull(result);
      Assert.Equal("NoShow", result.GetType().GetProperty("newStatus")?.GetValue(result)?.ToString());
      Assert.NotNull(customer.SuspendedUntil);
      Assert.True(customer.SuspendedUntil > DateTime.UtcNow);
    }
  }
}
