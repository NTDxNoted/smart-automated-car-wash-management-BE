using AutoWash.Application.DTOs.Admin;
using AutoWash.Application.Interfaces;
using AutoWash.Application.Services;
using AutoWash.Domain.Entities;
using AutoWash.Domain.Enums;
using AutoWash.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AutoWash.Tests.Application.Services
{
  public class ReportServiceTests
  {
    private static ApplicationDbContext CreateDbContext()
    {
      var options = new DbContextOptionsBuilder<ApplicationDbContext>()
          .UseInMemoryDatabase(Guid.NewGuid().ToString())
          .Options;

      return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetOverviewReportAsync_ReturnsMonthlySummaryAndRates()
    {
      using var db = CreateDbContext();
      var service = new ReportService(db);
      var now = DateTime.UtcNow;
      var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

      db.Customers.Add(new Customer { CustomerID = 1, FullName = "A", Phone = "0901", Role = "MEMBER", CreatedAt = monthStart });
      await db.SaveChangesAsync();

      db.Bookings.AddRange(
          new Booking { BookingID = 1, CustomerID = 1, ServiceID = 1, FinalAmount = 100m, Status = BookingStatus.Completed, CreatedAt = monthStart.AddDays(10) },
          new Booking { BookingID = 2, CustomerID = 1, ServiceID = 1, FinalAmount = 200m, Status = BookingStatus.Completed, CreatedAt = monthStart.AddDays(11) },
          new Booking { BookingID = 3, CustomerID = 1, ServiceID = 1, FinalAmount = 50m, Status = BookingStatus.NoShow, CreatedAt = monthStart.AddDays(12) },
          new Booking { BookingID = 4, CustomerID = 1, ServiceID = 1, FinalAmount = 75m, Status = BookingStatus.Cancelled, CreatedAt = monthStart.AddDays(13) },
          new Booking { BookingID = 5, CustomerID = 1, ServiceID = 1, FinalAmount = 25m, Status = BookingStatus.Failed, CreatedAt = monthStart.AddDays(14) }
      );
      await db.SaveChangesAsync();

      var result = await service.GetOverviewReportAsync();

      Assert.Equal(DateTime.UtcNow.ToString("yyyy-MM"), result.Period);
      Assert.Equal(5, result.TotalBookings);
      Assert.Equal(2, result.CompletedBookings);
      Assert.Equal(1, result.FailedBookings);
      Assert.Equal(1, result.NoShowBookings);
      Assert.Equal(1, result.CancelledBookings);
      Assert.Equal(300m, result.TotalRevenue);
      Assert.Equal(0.20m, result.NoShowRate);
      Assert.Equal(60m, result.AvgOrderValue);
    }

    [Fact]
    public async Task GetPopularServicesReportAsync_ReturnsRevenueAndContributionByService()
    {
      using var db = CreateDbContext();
      var service = new ReportService(db);
      var startDate = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
      var endDate = new DateTime(2025, 5, 31, 23, 59, 59, DateTimeKind.Utc);

      db.Services.AddRange(
          new Service { ServiceID = 1, ServiceName = "Basic Wash" },
          new Service { ServiceID = 2, ServiceName = "Premium Wash" }
      );

      db.Bookings.AddRange(
          new Booking { BookingID = 1, CustomerID = 1, ServiceID = 1, FinalAmount = 100m, Status = BookingStatus.Completed, CompletedAt = startDate.AddDays(2) },
          new Booking { BookingID = 2, CustomerID = 1, ServiceID = 1, FinalAmount = 150m, Status = BookingStatus.Completed, CompletedAt = startDate.AddDays(4) },
          new Booking { BookingID = 3, CustomerID = 1, ServiceID = 2, FinalAmount = 200m, Status = BookingStatus.Completed, CompletedAt = startDate.AddDays(6) },
          new Booking { BookingID = 4, CustomerID = 1, ServiceID = 2, FinalAmount = 50m, Status = BookingStatus.Pending, CompletedAt = startDate.AddDays(7) },
          new Booking { BookingID = 5, CustomerID = 1, ServiceID = 1, FinalAmount = 75m, Status = BookingStatus.Completed, CompletedAt = startDate.AddDays(40) }
      );

      db.Transactions.AddRange(
          new Transaction { TransactionID = 1, BookingID = 1, Status = TransactionStatus.Paid },
          new Transaction { TransactionID = 2, BookingID = 2, Status = TransactionStatus.Paid },
          new Transaction { TransactionID = 3, BookingID = 3, Status = TransactionStatus.Paid },
          new Transaction { TransactionID = 4, BookingID = 4, Status = TransactionStatus.Pending },
          new Transaction { TransactionID = 5, BookingID = 5, Status = TransactionStatus.Paid }
      );
      await db.SaveChangesAsync();

      var result = await service.GetPopularServicesReportAsync(startDate, endDate);

      Assert.Collection(result,
          item =>
          {
            Assert.Equal(1, item.ServiceId);
            Assert.Equal("Basic Wash", item.ServiceName);
            Assert.Equal(2, item.TotalWashes);
            Assert.Equal(250m, item.Revenue);
            Assert.Equal(55.56m, item.RevenueContributionPercentage);
          },
          item =>
          {
            Assert.Equal(2, item.ServiceId);
            Assert.Equal("Premium Wash", item.ServiceName);
            Assert.Equal(1, item.TotalWashes);
            Assert.Equal(200m, item.Revenue);
            Assert.Equal(44.44m, item.RevenueContributionPercentage);
          });
    }

    [Fact]
    public async Task GetLoyaltyStatsAsync_ReturnsPointInventoryAndExpirySummary()
    {
      using var db = CreateDbContext();
      var service = new ReportService(db);

      db.LoyaltyAccounts.AddRange(
          new LoyaltyAccount { LoyaltyID = 1, CustomerID = 1, TotalPoints = 1200 },
          new LoyaltyAccount { LoyaltyID = 2, CustomerID = 2, TotalPoints = 300 }
      );

      db.PointTransactions.AddRange(
          new PointTransaction { PointTxnID = 1, LoyaltyID = 1, Points = 150, Type = PointTransactionType.Earn, CreatedAt = DateTime.UtcNow.AddDays(-5), ExpiredAt = DateTime.UtcNow.AddDays(5) },
          new PointTransaction { PointTxnID = 2, LoyaltyID = 1, Points = 100, Type = PointTransactionType.Earn, CreatedAt = DateTime.UtcNow.AddDays(-10), ExpiredAt = DateTime.UtcNow.AddDays(-1) },
          new PointTransaction { PointTxnID = 3, LoyaltyID = 2, Points = 50, Type = PointTransactionType.Earn, CreatedAt = DateTime.UtcNow.AddDays(-10), ExpiredAt = DateTime.UtcNow.AddDays(30) }
      );
      await db.SaveChangesAsync();

      var result = await service.GetLoyaltyStatsAsync();

      Assert.Equal(1500, result.TotalPointsInCirculation);
      Assert.Equal(150, result.PointsExpiringSoon);
      Assert.Equal(100, result.ExpiredPoints);
    }
  }
}
