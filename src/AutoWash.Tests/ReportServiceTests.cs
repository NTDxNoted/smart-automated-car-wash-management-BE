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

      var result = await service.GetOverviewReportAsync("month", null, null);

      Assert.Equal("month", result.Period);
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

    [Fact]
    public async Task GetPopularServicesReportAsync_ShouldRankByUsageCountAndComputePercentage()
    {
      using var db = CreateDbContext();
      var service = new ReportService(db);

      db.Services.Add(new Service { ServiceID = 1, ServiceName = "Rửa cơ bản", ServiceCategory = "Basic", Description = "d", Price = 50000m, Duration = 30 });
      db.Services.Add(new Service { ServiceID = 2, ServiceName = "Rửa VIP", ServiceCategory = "VIP", Description = "d", Price = 150000m, Duration = 60 });
      db.Bookings.AddRange(
          new Booking { CustomerID = 1, ServiceID = 1, Status = BookingStatus.Completed, FinalAmount = 50000m, CreatedAt = DateTime.UtcNow },
          new Booking { CustomerID = 1, ServiceID = 1, Status = BookingStatus.Completed, FinalAmount = 50000m, CreatedAt = DateTime.UtcNow },
          new Booking { CustomerID = 1, ServiceID = 2, Status = BookingStatus.Completed, FinalAmount = 150000m, CreatedAt = DateTime.UtcNow },
          new Booking { CustomerID = 1, ServiceID = 2, Status = BookingStatus.Pending, FinalAmount = 150000m, CreatedAt = DateTime.UtcNow } // không tính vì chưa Completed
      );
      await db.SaveChangesAsync();

      var result = await service.GetPopularServicesReportAsync(null, null);

      Assert.Equal(2, result.Count);
      Assert.Equal("Rửa cơ bản", result[0].ServiceName); // 2 lượt > 1 lượt, xếp đầu
      Assert.Equal(2, result[0].UsageCount);
      Assert.Equal(100000m, result[0].TotalRevenue);
      Assert.Equal(66.67m, result[0].Percentage); // 2/3 completed bookings
    }

    [Fact]
    public async Task GetTierDistributionAsync_ShouldGroupCustomersByPointsBasedTier()
    {
      using var db = CreateDbContext();
      var service = new ReportService(db);

      db.Customers.Add(new Customer { FullName = "Platinum Cust", Phone = "0901", Password = "pw", CreatedAt = DateTime.UtcNow, LoyaltyAccount = new LoyaltyAccount { TotalPoints = 6000, LastUpdated = DateTime.UtcNow } });
      db.Customers.Add(new Customer { FullName = "Member Cust", Phone = "0902", Password = "pw", CreatedAt = DateTime.UtcNow, LoyaltyAccount = new LoyaltyAccount { TotalPoints = 10, LastUpdated = DateTime.UtcNow } });
      db.Customers.Add(new Customer { FullName = "No Loyalty Cust", Phone = "0903", Password = "pw", CreatedAt = DateTime.UtcNow });
      await db.SaveChangesAsync();

      var result = await service.GetTierDistributionAsync();

      Assert.Equal(2, result.Single(x => x.Tier == "Member").CustomerCount); // "no loyalty" + "10 điểm" đều rơi vào Member
      Assert.Equal(1, result.Single(x => x.Tier == "Platinum").CustomerCount);
    }

    [Fact]
    public async Task GetPeakOccupancyReportAsync_ShouldCountBookingsPerDayOfWeekAndHourSlot()
    {
      using var db = CreateDbContext();
      var service = new ReportService(db);

      // 2024-01-01 là Thứ Hai theo giờ VN (UTC+7); 08:00 VN = 01:00 UTC
      var mondayMorningUtc = new DateTime(2024, 1, 1, 1, 0, 0, DateTimeKind.Utc);
      db.Bookings.Add(new Booking { CustomerID = 1, ServiceID = 1, Status = BookingStatus.Completed, ScheduledTime = mondayMorningUtc, FinalAmount = 50000m, CreatedAt = mondayMorningUtc });
      await db.SaveChangesAsync();

      var result = await service.GetPeakOccupancyReportAsync(new DateTime(2024, 1, 1), new DateTime(2024, 1, 1));

      Assert.Equal(1, result.TotalDays);
      Assert.Equal(1, result.DayOfWeekStats.First(d => d.DayOfWeek == "Monday").BookingCount);
      Assert.Equal(0, result.DayOfWeekStats.First(d => d.DayOfWeek == "Tuesday").BookingCount);
      Assert.Contains(result.HourStats, h => h.BookingCount == 1);
    }

    [Fact]
    public async Task GetPromotionRoiReportAsync_ShouldComputeRoiFromCompletedPaidBookings()
    {
      using var db = CreateDbContext();
      var service = new ReportService(db);

      var completedAtVn = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc); // đã trong khoảng UTC test dưới

      db.Promotions.Add(new Promotion { PromotionID = 1, Title = "Tết Sale", PromoCode = "TET2024", DiscountType = "Fixed_Amount", DiscountValue = 20000m, StartDate = new DateTime(2024, 1, 1), EndDate = new DateTime(2024, 1, 31) });
      db.Bookings.Add(new Booking { BookingID = 1, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Completed, FinalAmount = 100000m, ScheduledTime = completedAtVn, CompletedAt = completedAtVn, CreatedAt = completedAtVn });
      db.CustomerPromotions.Add(new CustomerPromotion { CustomerID = 1, PromotionID = 1, BookingID = 1, DiscountAmountActual = 20000m, UsedAt = completedAtVn });
      db.Transactions.Add(new Transaction { BookingID = 1, Amount = 100000m, PaymentMethod = PaymentMethod.Cash, Status = TransactionStatus.Paid, PaidAt = completedAtVn });
      await db.SaveChangesAsync();

      var result = await service.GetPromotionRoiReportAsync(new DateTime(2024, 1, 1), new DateTime(2024, 1, 31));

      Assert.Equal(1, result.TotalPromotions);
      var item = result.Items.Single();
      Assert.Equal("TET2024", item.PromoCode);
      Assert.Equal(1, item.UsageCount);
      Assert.Equal(20000m, item.TotalDiscountGiven);
      Assert.Equal(100000m, item.RevenueGenerated);
      Assert.Equal(400m, item.RoiPercentage); // (100000-20000)/20000 * 100
    }
  }
}
