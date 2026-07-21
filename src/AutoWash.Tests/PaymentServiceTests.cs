using AutoWash.Application.DTOs.Admin;
using AutoWash.Application.Interfaces;
using AutoWash.Application.Services;
using AutoWash.Domain.Entities;
using AutoWash.Domain.Enums;
using AutoWash.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Xunit;

namespace AutoWash.Tests.Application.Services
{
  public class PaymentServiceTests
  {
    private static ApplicationDbContext CreateDbContext()
    {
      // PaymentService dùng Database.BeginTransactionAsync(); in-memory provider không hỗ trợ
      // transaction thật nên phải ignore warning này, nếu không EF ném InvalidOperationException.
      var options = new DbContextOptionsBuilder<ApplicationDbContext>()
          .UseInMemoryDatabase(Guid.NewGuid().ToString())
          .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
          .Options;

      return new ApplicationDbContext(options);
    }

    private static PaymentService CreateService(ApplicationDbContext dbContext, int pointsEarned = 10)
    {
      var pointServiceMock = new Mock<IPointService>();
      pointServiceMock.Setup(p => p.EarnPointsAsync(It.IsAny<int>())).ReturnsAsync(pointsEarned);

      var tierServiceMock = new Mock<ITierService>();
      tierServiceMock.Setup(t => t.EvaluateUpgradeAsync(It.IsAny<int>())).Returns(Task.CompletedTask);

      return new PaymentService(dbContext, pointServiceMock.Object, tierServiceMock.Object);
    }

    private static (Customer customer, Booking booking) SeedPendingBooking(ApplicationDbContext dbContext, decimal finalAmount = 100000m)
    {
      var customer = new Customer
      {
        FullName = "Test User",
        Phone = "0901234567",
        Password = "hashed",
        TotalSpending = 0m,
        CreatedAt = DateTime.UtcNow
      };
      dbContext.Customers.Add(customer);
      dbContext.SaveChanges();

      var booking = new Booking
      {
        CustomerID = customer.CustomerID,
        Phone = customer.Phone,
        VehicleID = 1,
        LicensePlate = "51A-123.45",
        ServiceID = 1,
        ScheduledTime = DateTime.UtcNow,
        Status = BookingStatus.Pending,
        BaseAmount = finalAmount,
        FinalAmount = finalAmount,
        CreatedAt = DateTime.UtcNow
      };
      dbContext.Bookings.Add(booking);
      dbContext.SaveChanges();

      return (customer, booking);
    }

    [Fact]
    public async Task RecordPaymentAsync_WithConfirmedCash_ShouldCompleteBookingAndReturnPaymentResponse()
    {
      using var dbContext = CreateDbContext();
      var (customer, booking) = SeedPendingBooking(dbContext, 100000m);
      var service = CreateService(dbContext, pointsEarned: 10);

      var response = await service.RecordPaymentAsync(booking.BookingID, new RecordPaymentRequest
      {
        PaymentMethod = "Cash",
        Confirmed = true
      });

      Assert.Equal(booking.BookingID, response.BookingId);
      Assert.Equal(100000m, response.Amount);
      Assert.Equal("Cash", response.PaymentMethod);
      Assert.Equal("Paid", response.Status);
      Assert.Equal("Completed", response.BookingStatus);
      Assert.Equal(10, response.Loyalty.PointsEarned);

      var persistedBooking = await dbContext.Bookings.FirstAsync(b => b.BookingID == booking.BookingID);
      Assert.Equal(BookingStatus.Completed, persistedBooking.Status);
      Assert.NotNull(persistedBooking.CompletedAt);

      var persistedCustomer = await dbContext.Customers.FirstAsync(c => c.CustomerID == customer.CustomerID);
      Assert.Equal(100000m, persistedCustomer.TotalSpending);
    }

    [Fact]
    public async Task RecordPaymentAsync_WithNonExistentBooking_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var service = CreateService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.RecordPaymentAsync(999, new RecordPaymentRequest
      {
        PaymentMethod = "Cash",
        Confirmed = true
      }));

      Assert.Equal("BOOKING_NOT_FOUND", ex.Message);
    }

    [Fact]
    public async Task RecordPaymentAsync_WithAlreadyCompletedBooking_ShouldThrowInvalidStatus()
    {
      using var dbContext = CreateDbContext();
      var (_, booking) = SeedPendingBooking(dbContext);
      booking.Status = BookingStatus.Completed;
      await dbContext.SaveChangesAsync();

      var service = CreateService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.RecordPaymentAsync(booking.BookingID, new RecordPaymentRequest
      {
        PaymentMethod = "Cash",
        Confirmed = true
      }));

      Assert.Equal("INVALID_STATUS", ex.Message);
    }

    [Fact]
    public async Task RecordPaymentAsync_WithInvalidPaymentMethod_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var (_, booking) = SeedPendingBooking(dbContext);
      var service = CreateService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.RecordPaymentAsync(booking.BookingID, new RecordPaymentRequest
      {
        PaymentMethod = "Bitcoin",
        Confirmed = true
      }));

      Assert.Equal("INVALID_PAYMENT_METHOD", ex.Message);
    }

    [Fact]
    public async Task RecordPaymentAsync_WithUnconfirmedCash_ShouldThrowCashNotConfirmed()
    {
      using var dbContext = CreateDbContext();
      var (_, booking) = SeedPendingBooking(dbContext);
      var service = CreateService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.RecordPaymentAsync(booking.BookingID, new RecordPaymentRequest
      {
        PaymentMethod = "Cash",
        Confirmed = false
      }));

      Assert.Equal("CASH_NOT_CONFIRMED", ex.Message);
    }

    [Fact]
    public async Task RecordPaymentAsync_WithUnconfirmedTransfer_ShouldThrowTransferNotConfirmed()
    {
      using var dbContext = CreateDbContext();
      var (_, booking) = SeedPendingBooking(dbContext);
      var service = CreateService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.RecordPaymentAsync(booking.BookingID, new RecordPaymentRequest
      {
        PaymentMethod = "Transfer",
        Confirmed = null
      }));

      Assert.Equal("TRANSFER_NOT_CONFIRMED", ex.Message);
    }

    [Fact]
    public async Task GetTransactionByBookingIdAsync_WithExistingTransaction_ShouldReturnIt()
    {
      using var dbContext = CreateDbContext();
      var (_, booking) = SeedPendingBooking(dbContext);
      dbContext.Transactions.Add(new Transaction
      {
        BookingID = booking.BookingID,
        Amount = booking.FinalAmount,
        PaymentMethod = PaymentMethod.Cash,
        PaidAt = DateTime.UtcNow,
        Status = TransactionStatus.Paid
      });
      await dbContext.SaveChangesAsync();

      var service = CreateService(dbContext);

      var transaction = await service.GetTransactionByBookingIdAsync(booking.BookingID);

      Assert.Equal(booking.BookingID, transaction.BookingID);
      Assert.Equal(TransactionStatus.Paid, transaction.Status);
    }

    [Fact]
    public async Task GetTransactionByBookingIdAsync_WithNoTransaction_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var service = CreateService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.GetTransactionByBookingIdAsync(999));

      Assert.Equal("TRANSACTION_NOT_FOUND", ex.Message);
    }
  }
}
