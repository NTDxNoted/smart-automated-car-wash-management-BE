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
      Assert.Equal(0.40m, result.CompletionRate);
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

    [Fact]
    public async Task GetCompletionRateDetailAsync_ComputesOverviewAndPendingProcessingSplit()
    {
      using var db = CreateDbContext();
      var service = new ReportService(db);

      var day = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

      db.Bookings.AddRange(
          new Booking { BookingID = 1, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Completed, CreatedAt = day.AddHours(1) },
          new Booking { BookingID = 2, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Completed, CreatedAt = day.AddHours(2) },
          new Booking { BookingID = 3, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Cancelled, CreatedAt = day.AddHours(3) },
          new Booking { BookingID = 4, CustomerID = 1, ServiceID = 1, Status = BookingStatus.NoShow, CreatedAt = day.AddHours(4) },
          new Booking { BookingID = 5, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Failed, CreatedAt = day.AddHours(5) },
          new Booking { BookingID = 6, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Pending, CheckInTime = null, CreatedAt = day.AddHours(6) },
          new Booking { BookingID = 7, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Pending, CheckInTime = day.AddHours(7), CreatedAt = day.AddHours(7) }
      );
      await db.SaveChangesAsync();

      var result = await service.GetCompletionRateDetailAsync(null, day, day, null, "day");

      Assert.Equal(7, result.Overview.TotalBookings);
      Assert.Equal(2, result.Overview.CompletedBookings);
      Assert.Equal(1, result.Overview.CancelledBookings);
      Assert.Equal(1, result.Overview.NoShowBookings);
      Assert.Equal(1, result.Overview.FailedBookings);
      Assert.Equal(1, result.Overview.PendingBookings);
      Assert.Equal(1, result.Overview.ProcessingBookings);
      Assert.Equal(2.0 / 7.0, (double)result.Overview.CompletionRate, 4);
      Assert.Equal(2, result.Formula.CompletedBookings);
      Assert.Equal(7, result.Formula.TotalBookings);

      var breakdownTotal = result.StatusBreakdown.Sum(x => x.Percentage);
      Assert.InRange(breakdownTotal, 99.9m, 100.1m);
    }

    [Fact]
    public async Task GetCompletionRateDetailAsync_TrendGroupsByDay()
    {
      using var db = CreateDbContext();
      var service = new ReportService(db);

      var start = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
      var end = new DateTime(2026, 8, 2, 0, 0, 0, DateTimeKind.Utc);

      db.Bookings.AddRange(
          new Booking { BookingID = 1, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Completed, CreatedAt = start.AddHours(1) },
          new Booking { BookingID = 2, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Cancelled, CreatedAt = start.AddHours(2) },
          new Booking { BookingID = 3, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Completed, CreatedAt = end.AddHours(1) },
          new Booking { BookingID = 4, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Completed, CreatedAt = end.AddHours(2) }
      );
      await db.SaveChangesAsync();

      var result = await service.GetCompletionRateDetailAsync(null, start, end, null, "day");

      Assert.Equal(2, result.Trend.Count);
      var day1 = result.Trend[0];
      Assert.Equal(2, day1.TotalBookings);
      Assert.Equal(50m, day1.CompletionRate);
      var day2 = result.Trend[1];
      Assert.Equal(2, day2.TotalBookings);
      Assert.Equal(100m, day2.CompletionRate);
    }

    [Fact]
    public async Task GetCompletionRateDetailAsync_FailureReasonsBreakdown()
    {
      using var db = CreateDbContext();
      var service = new ReportService(db);

      var day = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

      db.Bookings.AddRange(
          new Booking { BookingID = 1, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Completed, CreatedAt = day.AddHours(1) },
          new Booking { BookingID = 2, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Cancelled, CreatedAt = day.AddHours(2) },
          new Booking { BookingID = 3, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Cancelled, CreatedAt = day.AddHours(3) },
          new Booking { BookingID = 4, CustomerID = 1, ServiceID = 1, Status = BookingStatus.NoShow, CreatedAt = day.AddHours(4) },
          new Booking { BookingID = 5, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Failed, CreatedAt = day.AddHours(5) }
      );
      await db.SaveChangesAsync();

      var result = await service.GetCompletionRateDetailAsync(null, day, day, null, "day");

      var cancelled = result.FailureReasons.Single(x => x.Status == "Cancelled");
      var noShow = result.FailureReasons.Single(x => x.Status == "NoShow");
      var failed = result.FailureReasons.Single(x => x.Status == "Failed");

      // Mẫu 4 booking chưa hoàn thành (2 Cancelled, 1 NoShow, 1 Failed) — % tính trên nhóm này, không tính trên tổng.
      Assert.Equal(50m, cancelled.Percentage);
      Assert.Equal(25m, noShow.Percentage);
      Assert.Equal(25m, failed.Percentage);
    }

    [Fact]
    public async Task GetCompletionRateDetailAsync_TopServicesComputesPerServiceRate()
    {
      using var db = CreateDbContext();
      var service = new ReportService(db);

      var day = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

      db.Services.AddRange(
          new Service { ServiceID = 1, ServiceName = "Rửa thường", ServiceCategory = "Basic", Description = "d", Price = 50000m, Duration = 30, Status = "Active" },
          new Service { ServiceID = 2, ServiceName = "Rửa cao cấp", ServiceCategory = "Premium", Description = "d", Price = 150000m, Duration = 60, Status = "Active" }
      );
      db.Bookings.AddRange(
          new Booking { BookingID = 1, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Completed, CreatedAt = day.AddHours(1) },
          new Booking { BookingID = 2, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Cancelled, CreatedAt = day.AddHours(2) },
          new Booking { BookingID = 3, CustomerID = 1, ServiceID = 2, Status = BookingStatus.Completed, CreatedAt = day.AddHours(3) },
          new Booking { BookingID = 4, CustomerID = 1, ServiceID = 2, Status = BookingStatus.Completed, CreatedAt = day.AddHours(4) }
      );
      await db.SaveChangesAsync();

      var result = await service.GetCompletionRateDetailAsync(null, day, day, null, "day");

      var basic = result.TopServices.Single(x => x.ServiceId == 1);
      var premium = result.TopServices.Single(x => x.ServiceId == 2);

      Assert.Equal(50m, basic.CompletionRate);
      Assert.Equal(100m, premium.CompletionRate);
    }

    [Fact]
    public async Task GetCompletionRateDetailAsync_TopServicesFilteredByServiceId()
    {
      using var db = CreateDbContext();
      var service = new ReportService(db);

      var day = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

      db.Bookings.AddRange(
          new Booking { BookingID = 1, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Completed, CreatedAt = day.AddHours(1) },
          new Booking { BookingID = 2, CustomerID = 1, ServiceID = 2, Status = BookingStatus.Completed, CreatedAt = day.AddHours(2) }
      );
      await db.SaveChangesAsync();

      var result = await service.GetCompletionRateDetailAsync(null, day, day, 1, "day");

      Assert.Equal(1, result.Overview.TotalBookings);
    }

    [Fact]
    public async Task GetCompletionRateDetailAsync_TimeSlotsBucketByTwoHourVietnamWindow()
    {
      using var db = CreateDbContext();
      var service = new ReportService(db);

      var day = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

      // VN = UTC+7. UTC 02:00 -> VN 09:00 (slot 08:00-10:00). UTC 08:00 -> VN 15:00 (slot 14:00-16:00).
      db.Bookings.AddRange(
          new Booking { BookingID = 1, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Completed, ScheduledTime = day.AddHours(2), CreatedAt = day.AddHours(2) },
          new Booking { BookingID = 2, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Cancelled, ScheduledTime = day.AddHours(2), CreatedAt = day.AddHours(2) },
          new Booking { BookingID = 3, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Completed, ScheduledTime = day.AddHours(8), CreatedAt = day.AddHours(8) }
      );
      await db.SaveChangesAsync();

      var result = await service.GetCompletionRateDetailAsync(null, day, day, null, "day");

      var morningSlot = result.TimeSlots.Single(x => x.TimeSlot == "08:00-10:00");
      var afternoonSlot = result.TimeSlots.Single(x => x.TimeSlot == "14:00-16:00");

      Assert.Equal(2, morningSlot.TotalBookings);
      Assert.Equal(50m, morningSlot.CompletionRate);
      Assert.Equal(1, afternoonSlot.TotalBookings);
      Assert.Equal(100m, afternoonSlot.CompletionRate);
    }

    [Fact]
    public async Task GetCompletionRateDetailAsync_AlertsTriggerAtDocumentedThresholds()
    {
      using var db = CreateDbContext();
      var service = new ReportService(db);

      var start = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
      var previousDay = start.AddDays(-1);

      // Kỳ trước: 2 booking hủy — làm mốc so sánh cho cảnh báo tăng đột biến tỷ lệ hủy.
      db.Bookings.AddRange(
          new Booking { BookingID = 100, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Cancelled, CreatedAt = previousDay.AddHours(1) },
          new Booking { BookingID = 101, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Cancelled, CreatedAt = previousDay.AddHours(2) },
          new Booking { BookingID = 102, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Completed, CreatedAt = previousDay.AddHours(3) }
      );

      // Kỳ hiện tại: completion rate 20% (<80%), no-show 30% (>10%), 3 booking hủy so với 2 kỳ trước (+50% > 20%).
      db.Bookings.AddRange(
          new Booking { BookingID = 1, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Completed, CreatedAt = start.AddHours(1) },
          new Booking { BookingID = 2, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Cancelled, CreatedAt = start.AddHours(2) },
          new Booking { BookingID = 3, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Cancelled, CreatedAt = start.AddHours(3) },
          new Booking { BookingID = 4, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Cancelled, CreatedAt = start.AddHours(4) },
          new Booking { BookingID = 5, CustomerID = 1, ServiceID = 1, Status = BookingStatus.NoShow, CreatedAt = start.AddHours(5) },
          new Booking { BookingID = 6, CustomerID = 1, ServiceID = 1, Status = BookingStatus.NoShow, CreatedAt = start.AddHours(6) },
          new Booking { BookingID = 7, CustomerID = 1, ServiceID = 1, Status = BookingStatus.NoShow, CreatedAt = start.AddHours(7) },
          new Booking { BookingID = 8, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Pending, CreatedAt = start.AddHours(8) },
          new Booking { BookingID = 9, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Pending, CreatedAt = start.AddHours(9) },
          new Booking { BookingID = 10, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Pending, CreatedAt = start.AddHours(10) }
      );
      await db.SaveChangesAsync();

      var result = await service.GetCompletionRateDetailAsync(null, start, start, null, "day");

      Assert.Contains(result.Alerts, a => a.Code == "LOW_COMPLETION_RATE");
      Assert.Contains(result.Alerts, a => a.Code == "HIGH_NOSHOW_RATE");
      Assert.Contains(result.Alerts, a => a.Code == "CANCELLATION_SPIKE");
    }

    [Fact]
    public async Task GetCompletionRateDetailAsync_PeriodComparisonAndKpiLevel()
    {
      using var db = CreateDbContext();
      var service = new ReportService(db);

      var start = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
      var previousDay = start.AddDays(-1);

      // Kỳ trước: 1/2 hoàn thành = 50%.
      db.Bookings.AddRange(
          new Booking { BookingID = 100, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Completed, CreatedAt = previousDay.AddHours(1) },
          new Booking { BookingID = 101, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Cancelled, CreatedAt = previousDay.AddHours(2) }
      );

      // Kỳ hiện tại: 19/20 hoàn thành = 95% -> KPI "Excellent", tăng 45 điểm % so với kỳ trước.
      for (var i = 1; i <= 19; i++)
      {
        db.Bookings.Add(new Booking { BookingID = i, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Completed, CreatedAt = start.AddMinutes(i) });
      }
      db.Bookings.Add(new Booking { BookingID = 20, CustomerID = 1, ServiceID = 1, Status = BookingStatus.Cancelled, CreatedAt = start.AddMinutes(20) });
      await db.SaveChangesAsync();

      var result = await service.GetCompletionRateDetailAsync(null, start, start, null, "day");

      Assert.Equal(95m, result.PeriodComparison.CurrentCompletionRate);
      Assert.Equal(50m, result.PeriodComparison.PreviousCompletionRate);
      Assert.Equal(45m, result.PeriodComparison.DeltaPercentagePoints);
      Assert.Equal("Up", result.PeriodComparison.TrendDirection);
      Assert.Equal("Excellent", result.Kpi.Level);
    }

    [Fact]
    public async Task GetUnfinishedBookingsAsync_ExcludesCompletedAndPaginates()
    {
      using var db = CreateDbContext();
      var service = new ReportService(db);

      var day = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

      db.Bookings.AddRange(
          new Booking { BookingID = 1, CustomerID = 1, ServiceID = 1, Phone = "0901", Status = BookingStatus.Completed, ScheduledTime = day.AddHours(1), CreatedAt = day.AddHours(1) },
          new Booking { BookingID = 2, CustomerID = 1, ServiceID = 1, Phone = "0902", Status = BookingStatus.Cancelled, ScheduledTime = day.AddHours(2), CreatedAt = day.AddHours(2) },
          new Booking { BookingID = 3, CustomerID = 1, ServiceID = 1, Phone = "0903", Status = BookingStatus.NoShow, ScheduledTime = day.AddHours(3), CreatedAt = day.AddHours(3) }
      );
      await db.SaveChangesAsync();

      var page1 = await service.GetUnfinishedBookingsAsync(null, day, day, null, page: 1, pageSize: 1);

      Assert.Equal(2, page1.TotalCount);
      Assert.Single(page1.Items);
      // OrderByDescending(ScheduledTime) -> booking 3 (giờ muộn nhất) đứng đầu trang 1.
      Assert.Equal(3, page1.Items[0].BookingId);
      Assert.All(page1.Items, i => Assert.NotEqual("Completed", i.Status));
    }

    [Fact]
    public async Task ExportUnfinishedBookingsAsync_ReturnsValidXlsxBytes()
    {
      using var db = CreateDbContext();
      var service = new ReportService(db);

      var day = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

      db.Bookings.Add(new Booking { BookingID = 1, CustomerID = 1, ServiceID = 1, Phone = "0901", Status = BookingStatus.Cancelled, ScheduledTime = day, CreatedAt = day });
      await db.SaveChangesAsync();

      var bytes = await service.ExportUnfinishedBookingsAsync(null, day, day, null);

      Assert.NotEmpty(bytes);
      // .xlsx là file zip -> 2 byte đầu phải là chữ ký "PK" (0x50 0x4B).
      Assert.Equal(0x50, bytes[0]);
      Assert.Equal(0x4B, bytes[1]);
    }
  }
}
