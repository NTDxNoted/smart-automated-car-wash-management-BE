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
    public async Task GetPeakOccupancyReportAsync_ShouldCountEarlyMorningVietnamBookingOnRequestedDay()
    {
      using var db = CreateDbContext();
      var service = new ReportService(db);

      // 2026-07-10 00:30 giờ VN == 2026-07-09 17:30 UTC (lưu trong DB). Nếu range không dịch -7h,
      // mốc UTC này rơi trước rangeStart (2026-07-10 00:00 UTC) và bị loại khỏi báo cáo ngày 10/07.
      var scheduledUtc = new DateTime(2026, 7, 9, 17, 30, 0, DateTimeKind.Utc);
      db.Bookings.Add(new Booking
      {
        BookingID = 1,
        CustomerID = 1,
        ServiceID = 1,
        Phone = "0901111111",
        LicensePlate = "51A-111.11",
        ScheduledTime = scheduledUtc,
        Status = BookingStatus.Completed,
        FinalAmount = 100000m,
        CreatedAt = scheduledUtc
      });
      await db.SaveChangesAsync();

      var requestedDay = new DateTime(2026, 7, 10);
      var result = await service.GetPeakOccupancyReportAsync(requestedDay, requestedDay);

      // Booking 00:30 giờ VN nằm ngoài khung slot 07:30-17:30 nên HourStats không đếm (đúng thiết
      // kế) — chỉ DayOfWeekStats (không giới hạn giờ) mới phản ánh đúng ngày lịch VN của booking.
      Assert.Equal(1, result.DayOfWeekStats.Sum(d => d.BookingCount));
    }

    [Fact]
    public async Task GetPopularServicesReportAsync_ShouldMarkDeletedServiceInName()
    {
      using var db = CreateDbContext();
      var service = new ReportService(db);

      db.Services.Add(new Service { ServiceID = 1, ServiceName = "Rửa cơ bản", ServiceCategory = "Basic", Description = "d", Price = 50000m, Duration = 30, Status = "Deleted" });
      db.Bookings.Add(new Booking { BookingID = 1, CustomerID = 1, ServiceID = 1, FinalAmount = 100m, Status = BookingStatus.Completed, CreatedAt = DateTime.UtcNow });
      await db.SaveChangesAsync();

      var result = await service.GetPopularServicesReportAsync(null, null);

      Assert.Equal("Rửa cơ bản (Đã xóa)", result.Single().ServiceName);
    }

    [Fact]
    public async Task GetRevenueDetailReportAsync_ComputesGrossDiscountNetAndPaymentMethodSplit()
    {
      using var db = CreateDbContext();
      var service = new ReportService(db);

      var today = DateTime.UtcNow.Date;

      db.Customers.Add(new Customer { CustomerID = 1, FullName = "Nguyen Van A", Phone = "0901111111", CreatedAt = today });
      db.Services.Add(new Service { ServiceID = 1, ServiceName = "Rửa cơ bản", ServiceCategory = "Basic", Description = "d", Price = 100000m, Duration = 30, Status = "Active" });
      db.Promotions.Add(new Promotion { PromotionID = 1, Title = "Giảm hè", PromoCode = "SUMMER10", DiscountType = "Fixed_Amount", DiscountValue = 20000m, StartDate = today.AddDays(-30), EndDate = today.AddDays(30) });
      await db.SaveChangesAsync();

      // Booking 1: Cash, có áp dụng khuyến mãi
      db.Bookings.Add(new Booking
      {
        BookingID = 1,
        CustomerID = 1,
        Phone = "0901111111",
        ServiceID = 1,
        PromotionID = 1,
        Status = BookingStatus.Completed,
        BaseAmount = 100000m,
        DiscountApplied = 20000m,
        FinalAmount = 80000m,
        CreatedAt = today
      });

      // Booking 2: Transfer, không khuyến mãi
      db.Bookings.Add(new Booking
      {
        BookingID = 2,
        CustomerID = 1,
        Phone = "0901111111",
        ServiceID = 1,
        Status = BookingStatus.Completed,
        BaseAmount = 150000m,
        DiscountApplied = 0m,
        FinalAmount = 150000m,
        CreatedAt = today
      });
      await db.SaveChangesAsync();

      db.Transactions.AddRange(
          new Transaction { TransactionID = 1, BookingID = 1, Amount = 80000m, PaymentMethod = PaymentMethod.Cash, PaidAt = today.AddHours(3), Status = TransactionStatus.Paid },
          new Transaction { TransactionID = 2, BookingID = 2, Amount = 150000m, PaymentMethod = PaymentMethod.Transfer, PaidAt = today.AddHours(4), Status = TransactionStatus.Paid }
      );
      await db.SaveChangesAsync();

      var result = await service.GetRevenueDetailReportAsync(today, today, null);

      Assert.Equal(250000m, result.GrossRevenue);
      Assert.Equal(20000m, result.TotalDiscount);
      Assert.Equal(230000m, result.NetRevenue);
      Assert.Equal(80000m, result.CashRevenue);
      Assert.Equal(150000m, result.TransferRevenue);
      Assert.Equal(result.CashRevenue + result.TransferRevenue, result.NetRevenue);
      Assert.Equal(2, result.Transactions.Count);

      var cashItem = result.Transactions.Single(t => t.PaymentMethod == "Cash");
      Assert.Equal("Nguyen Van A", cashItem.CustomerName);
      Assert.Equal("Rửa cơ bản", cashItem.ServiceName);
      Assert.Equal("Giảm hè (SUMMER10)", cashItem.PromotionApplied);
      Assert.Equal(20000m, cashItem.DiscountAmount);

      var transferItem = result.Transactions.Single(t => t.PaymentMethod == "Transfer");
      Assert.Null(transferItem.PromotionApplied);
      Assert.Equal(0m, transferItem.DiscountAmount);

      // Admin phải cộng đúng cột Khuyến mãi trên bảng ra bằng số Tổng khuyến mãi phía trên —
      // không được lệch giữa 2 nguồn dữ liệu này.
      Assert.Equal(result.TotalDiscount, result.Transactions.Sum(t => t.DiscountAmount));
    }

    [Fact]
    public async Task GetRevenueDetailReportAsync_FiltersByPaymentMethod()
    {
      using var db = CreateDbContext();
      var service = new ReportService(db);

      var today = DateTime.UtcNow.Date;

      db.Customers.Add(new Customer { CustomerID = 1, FullName = "Nguyen Van B", Phone = "0902222222", CreatedAt = today });
      db.Services.Add(new Service { ServiceID = 1, ServiceName = "Rửa cơ bản", ServiceCategory = "Basic", Description = "d", Price = 100000m, Duration = 30, Status = "Active" });
      await db.SaveChangesAsync();

      db.Bookings.AddRange(
          new Booking { BookingID = 1, CustomerID = 1, Phone = "0902222222", ServiceID = 1, Status = BookingStatus.Completed, BaseAmount = 100000m, FinalAmount = 100000m, CreatedAt = today },
          new Booking { BookingID = 2, CustomerID = 1, Phone = "0902222222", ServiceID = 1, Status = BookingStatus.Completed, BaseAmount = 200000m, FinalAmount = 200000m, CreatedAt = today }
      );
      await db.SaveChangesAsync();

      db.Transactions.AddRange(
          new Transaction { TransactionID = 1, BookingID = 1, Amount = 100000m, PaymentMethod = PaymentMethod.Cash, PaidAt = today.AddHours(2), Status = TransactionStatus.Paid },
          new Transaction { TransactionID = 2, BookingID = 2, Amount = 200000m, PaymentMethod = PaymentMethod.Transfer, PaidAt = today.AddHours(3), Status = TransactionStatus.Paid }
      );
      await db.SaveChangesAsync();

      var result = await service.GetRevenueDetailReportAsync(today, today, "cash");

      Assert.Equal(100000m, result.GrossRevenue);
      Assert.Equal(100000m, result.NetRevenue);
      Assert.Equal(100000m, result.CashRevenue);
      Assert.Equal(0m, result.TransferRevenue);
      Assert.Single(result.Transactions);
    }
  }
}
