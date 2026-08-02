using AutoWash.Application.DTOs;
using AutoWash.Application.DTOs.Admin;
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
    public async Task GetCustomersAsync_ShouldExcludeAdminAccounts()
    {
      using var dbContext = CreateDbContext();

      dbContext.Customers.AddRange(
          new Customer { CustomerID = 1, FullName = "Member", Phone = "0901111111", Password = "pw", Role = "MEMBER", CreatedAt = DateTime.UtcNow },
          new Customer { CustomerID = 2, FullName = "Admin", Phone = "0902222222", Password = "pw", Role = "ADMIN", CreatedAt = DateTime.UtcNow });
      await dbContext.SaveChangesAsync();

      var service = new AdminCustomerService(dbContext);
      var result = await service.GetCustomersAsync(null, null, 1, 10);

      Assert.Single(result.Data);
      Assert.Equal("Member", result.Data[0].FullName);
    }

    [Fact]
    public async Task GetCustomerByIdAsync_WithAdminAccount_ShouldThrowNotFound()
    {
      using var dbContext = CreateDbContext();

      dbContext.Customers.Add(new Customer { CustomerID = 1, FullName = "Admin", Phone = "0901111111", Password = "pw", Role = "ADMIN", CreatedAt = DateTime.UtcNow });
      await dbContext.SaveChangesAsync();

      var service = new AdminCustomerService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.GetCustomerByIdAsync(1));
      Assert.StartsWith("NOT_FOUND", ex.Message);
    }

    [Fact]
    public async Task ToggleLockCustomerAsync_WithAdminAccount_ShouldThrowForbidden()
    {
      using var dbContext = CreateDbContext();

      dbContext.Customers.Add(new Customer { CustomerID = 1, FullName = "Admin", Phone = "0901111111", Password = "pw", Role = "ADMIN", IsLocked = false, CreatedAt = DateTime.UtcNow });
      await dbContext.SaveChangesAsync();

      var service = new AdminCustomerService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.ToggleLockCustomerAsync(1));
      Assert.StartsWith("FORBIDDEN", ex.Message);

      var persisted = await dbContext.Customers.FirstAsync(c => c.CustomerID == 1);
      Assert.False(persisted.IsLocked); // không bị thay đổi khi bị chặn
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
        ScheduledTime = DateTime.UtcNow.AddMinutes(-20),
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

    [Fact]
    public async Task UpdateBookingStatusAsync_WhenGuestNoShow_ShouldNotSuspendSharedGuestAccount()
    {
      using var dbContext = CreateDbContext();

      // Tài khoản "Khách vãng lai" dùng chung mà BookingService gán cho mọi booking không đăng nhập.
      var guestAccount = new Customer
      {
        CustomerID = 1,
        FullName = "Khách vãng lai",
        Phone = "GUEST",
        Password = "GUEST",
        Role = "MEMBER",
        CreatedAt = DateTime.UtcNow
      };
      dbContext.Customers.Add(guestAccount);

      // Hai khách lạ khác nhau (số điện thoại khác nhau) từng no-show, đều trỏ CustomerID=1 (guest chung).
      dbContext.Bookings.AddRange(
          new Booking
          {
            BookingID = 1,
            CustomerID = 1,
            Phone = "0911111111",
            ServiceID = 1,
            LicensePlate = "51A-111.11",
            ScheduledTime = DateTime.UtcNow.AddDays(-20),
            Status = BookingStatus.NoShow,
            CompletedAt = DateTime.UtcNow.AddDays(-20),
            CreatedAt = DateTime.UtcNow.AddDays(-20)
          },
          new Booking
          {
            BookingID = 2,
            CustomerID = 1,
            Phone = "0922222222",
            ServiceID = 1,
            LicensePlate = "51A-222.22",
            ScheduledTime = DateTime.UtcNow.AddDays(-10),
            Status = BookingStatus.NoShow,
            CompletedAt = DateTime.UtcNow.AddDays(-10),
            CreatedAt = DateTime.UtcNow.AddDays(-10)
          }
      );

      // Khách lạ thứ 3 (số điện thoại khác) đang no-show — không liên quan đến 2 khách trên.
      dbContext.Bookings.Add(new Booking
      {
        BookingID = 3,
        CustomerID = 1,
        Phone = "0933333333",
        ServiceID = 1,
        LicensePlate = "51A-333.33",
        ScheduledTime = DateTime.UtcNow.AddMinutes(-20),
        Status = BookingStatus.Pending,
        CreatedAt = DateTime.UtcNow
      });

      await dbContext.SaveChangesAsync();

      var logger = Mock.Of<ILogger<AdminBookingService>>();
      var service = new AdminBookingService(dbContext, logger);

      await service.UpdateBookingStatusAsync(3, new UpdateBookingStatusRequest
      {
        NewStatus = "NoShow"
      });

      // Guest dùng chung không được khóa/suspend dựa trên no-show của các khách lạ không liên quan.
      Assert.False(guestAccount.IsLocked);
      Assert.Null(guestAccount.SuspendedUntil);
    }

    [Fact]
    public async Task UpdateBookingStatusAsync_WhenCompleted_ShouldUpdateSpendingAndTriggerTierAndPoints()
    {
      using var dbContext = CreateDbContext();

      var customer = new Customer
      {
        CustomerID = 1,
        FullName = "Test Customer",
        Phone = "0903333333",
        Password = "pw",
        TotalSpending = 100000m,
        CreatedAt = DateTime.UtcNow
      };
      dbContext.Customers.Add(customer);

      dbContext.Bookings.Add(new Booking
      {
        BookingID = 1,
        CustomerID = 1,
        Phone = "0903333333",
        ServiceID = 1,
        LicensePlate = "51A-222.22",
        ScheduledTime = DateTime.UtcNow.AddMinutes(-10),
        Status = BookingStatus.Pending,
        FinalAmount = 250000m,
        CreatedAt = DateTime.UtcNow
      });

      await dbContext.SaveChangesAsync();

      var logger = Mock.Of<ILogger<AdminBookingService>>();
      var tierServiceMock = new Mock<ITierService>();
      var pointServiceMock = new Mock<IPointService>();
      var service = new AdminBookingService(dbContext, logger, adminNotifier: null, tierService: tierServiceMock.Object, pointService: pointServiceMock.Object);

      await service.UpdateBookingStatusAsync(1, new UpdateBookingStatusRequest
      {
        NewStatus = "Completed"
      });

      Assert.Equal(350000m, customer.TotalSpending);
      tierServiceMock.Verify(t => t.EvaluateUpgradeAsync(1), Times.Once);
      pointServiceMock.Verify(p => p.EarnPointsAsync(1), Times.Once);
    }
  }
}
