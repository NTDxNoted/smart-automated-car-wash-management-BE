using AutoWash.Application.DTOs;
using AutoWash.Application.DTOs.Admin;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AutoWash.Application.Services
{
  public class ReportService : IReportService
  {
    private readonly IApplicationDbContext _context;
    private readonly BookingSettings _bookingSettings;

    public ReportService(IApplicationDbContext context, IOptions<BookingSettings>? bookingSettings = null)
    {
      _context = context;
      _bookingSettings = bookingSettings?.Value ?? new BookingSettings { MaxParallelSlots = 3 };
    }

    public async Task<OverviewReportResponse> GetOverviewReportAsync(string filterType, DateTime? startDate, DateTime? endDate)
    {
      var now = DateTime.UtcNow;
      DateTime start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
      DateTime end = start.AddMonths(1);

      if (!string.IsNullOrEmpty(filterType))
      {
          switch (filterType.ToLower())
          {
              case "day":
                  start = DateTime.UtcNow.Date;
                  end = start.AddDays(1);
                  break;
              case "week":
                  int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
                  start = now.AddDays(-1 * diff).Date;
                  end = start.AddDays(7);
                  break;
              case "month":
                  start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                  end = start.AddMonths(1);
                  break;
              case "year":
                  start = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                  end = start.AddYears(1);
                  break;
          }
      }
      else if (startDate.HasValue && endDate.HasValue)
      {
          start = startDate.Value.Date;
          end = endDate.Value.Date.AddDays(1);
      }

      var bookings = await _context.Bookings
          .Where(b => b.CreatedAt >= start && b.CreatedAt < end)
          .ToListAsync();

      var totalBookings = bookings.Count;
      var completedBookings = bookings.Count(b => b.Status == BookingStatus.Completed);
      var failedBookings = bookings.Count(b => b.Status == BookingStatus.Failed);
      var noShowBookings = bookings.Count(b => b.Status == BookingStatus.NoShow);
      var cancelledBookings = bookings.Count(b => b.Status == BookingStatus.Cancelled);
      var totalRevenue = bookings.Where(b => b.Status == BookingStatus.Completed).Sum(b => b.FinalAmount);

      return new OverviewReportResponse
      {
        Period = filterType ?? "custom",
        TotalBookings = totalBookings,
        CompletedBookings = completedBookings,
        FailedBookings = failedBookings,
        NoShowBookings = noShowBookings,
        CancelledBookings = cancelledBookings,
        TotalRevenue = totalRevenue,
        NoShowRate = totalBookings == 0 ? 0m : Math.Round((decimal)noShowBookings / totalBookings, 4),
        AvgOrderValue = totalBookings == 0 ? 0m : Math.Round(totalRevenue / totalBookings, 2)
      };
    }

    public async Task<IReadOnlyList<PopularServiceResponse>> GetPopularServicesReportAsync(DateTime? startDate, DateTime? endDate)
    {
      var query = _context.Bookings
          .Where(b => b.Status == BookingStatus.Completed);

      if (startDate.HasValue)
      {
          query = query.Where(b => b.CreatedAt >= startDate.Value.Date);
      }
      if (endDate.HasValue)
      {
          query = query.Where(b => b.CreatedAt < endDate.Value.Date.AddDays(1));
      }

      var completedBookings = await query.ToListAsync();
      var totalCount = completedBookings.Count;

      var services = await _context.Services.ToListAsync();

      var result = completedBookings
          .GroupBy(b => b.ServiceID)
          .Select(g => {
              var service = services.FirstOrDefault(s => s.ServiceID == g.Key);
              var serviceName = service?.ServiceName ?? "Dịch vụ không xác định";
              if (service?.Status == "Deleted")
              {
                  serviceName = $"{serviceName} (Đã xóa)";
              }
              return new PopularServiceResponse
              {
                  ServiceId = g.Key,
                  ServiceName = serviceName,
                  UsageCount = g.Count(),
                  TotalRevenue = g.Sum(b => b.FinalAmount),
                  Percentage = totalCount == 0 ? 0m : Math.Round((decimal)g.Count() * 100m / totalCount, 2)
              };
          })
          .OrderByDescending(x => x.UsageCount)
          .ToList();

      return result.AsReadOnly();
    }

    public async Task<IReadOnlyList<RfmReportResponse>> GetRfmReportAsync()
    {
      var rows = await _context.VwCustomerRfm
          .AsNoTracking()
          .Select(x => new RfmReportResponse
          {
            CustomerId = x.CustomerId,
            FullName = x.FullName,
            Phone = x.Phone,
            CurrentTier = x.CurrentTier,
            RecencyDays = x.RecencyDays,
            Frequency = x.Frequency,
            MonetaryTotal = x.MonetaryTotal,
            TotalPoints = x.TotalPoints,
            TotalSpending = x.TotalSpending,
            MemberSince = x.MemberSince
          })
          .OrderByDescending(x => x.MonetaryTotal)
          .ToListAsync();

      return rows;
    }

    public async Task<IReadOnlyList<TierDistributionResponse>> GetTierDistributionAsync()
    {
      // Tier thật của khách là Customer.TierID (qua bảng Tiers, admin-configurable),
      // không phải suy ra từ điểm thưởng — 2 trục độc lập, xem CustomerService.GetProfileAsync.
      var tierNames = await _context.Tiers.ToDictionaryAsync(t => t.TierID, t => t.TierName);
      var customers = await _context.Customers.ToListAsync();

      var totalCustomers = customers.Count;
      var distribution = customers
          .GroupBy(c => tierNames.TryGetValue(c.TierID, out var name) ? name : (c.TierID == 1 ? "Member" : c.TierID.ToString()))
          .Select(g => new TierDistributionResponse
          {
            Tier = g.Key,
            CustomerCount = g.Count(),
            Percentage = totalCustomers == 0 ? 0m : Math.Round((decimal)g.Count() * 100m / totalCustomers, 2)
          })
          .OrderByDescending(x => x.CustomerCount)
          .ToList();

      return distribution;
    }

    public async Task<LoyaltyStatsResponse> GetLoyaltyStatsAsync()
    {
      var loyaltyAccounts = await _context.LoyaltyAccounts
          .Include(x => x.PointTransactions)
          .AsNoTracking()
          .ToListAsync();

      var totalPoints = loyaltyAccounts.Sum(x => x.TotalPoints);
      var expiringSoon = loyaltyAccounts
          .SelectMany(x => x.PointTransactions.Where(pt => pt.ExpiredAt.HasValue && pt.ExpiredAt.Value >= DateTime.UtcNow && pt.ExpiredAt.Value <= DateTime.UtcNow.AddDays(7)))
          .Sum(pt => pt.Points);
      var expired = loyaltyAccounts
          .SelectMany(x => x.PointTransactions.Where(pt => pt.ExpiredAt.HasValue && pt.ExpiredAt.Value < DateTime.UtcNow))
          .Sum(pt => pt.Points);

      return new LoyaltyStatsResponse
      {
        TotalPointsInCirculation = totalPoints,
        PointsExpiringSoon = expiringSoon,
        ExpiredPoints = expired
      };
    }

    // Vietnam is UTC+7 with no DST — fixed offset is safe.
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    public async Task<PeakOccupancyResponse> GetPeakOccupancyReportAsync(DateTime startDate, DateTime endDate)
    {
      int maxParallelSlots = _bookingSettings.MaxParallelSlots > 0 ? _bookingSettings.MaxParallelSlots : 1;
      var eligibleStatuses = new[] { BookingStatus.Completed, BookingStatus.Pending };

      // ScheduledTime lưu dưới dạng UTC; dịch biên ngày VN sang UTC trước khi so sánh, giống
      // GetPromotionRoiReportAsync bên dưới — nếu không, booking VN 00:00-07:00 bị loại và booking
      // sáng sớm hôm sau lại bị tính nhầm vào ngày trước.
      var rangeStart = startDate.Date.AddHours(-7);
      var rangeEnd = endDate.Date.AddDays(1).AddHours(-7);
      var totalDays = (endDate.Date - startDate.Date).Days + 1;

      var bookings = await _context.Bookings
          .Where(b => b.ScheduledTime >= rangeStart
                   && b.ScheduledTime < rangeEnd
                   && eligibleStatuses.Contains(b.Status))
          .ToListAsync();

      var dayOrder = new[]
      {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
        DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
      };

      var dayStats = dayOrder
          .Select(day => new DayOfWeekStatDto
          {
            DayOfWeek = day.ToString(),
            BookingCount = bookings.Count(b => b.ScheduledTime.Add(VietnamOffset).DayOfWeek == day)
          })
          .ToList();

      var hourStats = new List<HourStatDto>();
      var slotStart = new TimeSpan(7, 30, 0);
      var slotEnd = new TimeSpan(17, 30, 0);
      var current = slotStart;

      while (current <= slotEnd)
      {
        var next = current.Add(TimeSpan.FromMinutes(30));
        var count = bookings.Count(b =>
        {
          var t = b.ScheduledTime.Add(VietnamOffset).TimeOfDay;
          return t >= current && t < next;
        });

        var denominator = totalDays * maxParallelSlots;
        var occupancy = denominator == 0 ? 0m
            : Math.Round((decimal)count / denominator * 100, 2);

        hourStats.Add(new HourStatDto
        {
          TimeSlot = $"{(int)current.TotalHours:D2}:{current.Minutes:D2}",
          BookingCount = count,
          OccupancyPercentage = occupancy
        });

        current = next;
      }

      return new PeakOccupancyResponse
      {
        StartDate = startDate.ToString("yyyy-MM-dd"),
        EndDate = endDate.ToString("yyyy-MM-dd"),
        TotalDays = totalDays,
        MaxParallelSlots = maxParallelSlots,
        DayOfWeekStats = dayStats,
        HourStats = hourStats
      };
    }

    public async Task<PromotionRoiResponse> GetPromotionRoiReportAsync(DateTime startDate, DateTime endDate)
    {
      // CompletedAt is stored as UTC; shift VN date boundaries to UTC before comparing
      var rangeStart = startDate.Date.AddHours(-7);
      var rangeEnd = endDate.Date.AddDays(1).AddHours(-7);

      var rawItems = await (
          from cp in _context.CustomerPromotions
          join p in _context.Promotions on cp.PromotionID equals p.PromotionID
          join b in _context.Bookings on cp.BookingID equals b.BookingID
          join t in _context.Transactions on b.BookingID equals t.BookingID
          where b.Status == BookingStatus.Completed
             && t.Status == TransactionStatus.Paid
             && b.CompletedAt >= rangeStart
             && b.CompletedAt < rangeEnd
          group new { cp.DiscountAmountActual, b.FinalAmount }
            by new { p.PromotionID, p.Title, p.PromoCode } into g
          select new
          {
            g.Key.PromotionID,
            g.Key.Title,
            g.Key.PromoCode,
            UsageCount = g.Count(),
            TotalDiscountGiven = g.Sum(x => x.DiscountAmountActual),
            RevenueGenerated = g.Sum(x => x.FinalAmount)
          }
      ).ToListAsync();

      var items = rawItems
          .OrderByDescending(x => x.RevenueGenerated)
          .Select(x => new PromotionRoiItemDto
          {
            PromotionId = x.PromotionID,
            Title = x.Title,
            PromoCode = x.PromoCode,
            UsageCount = x.UsageCount,
            TotalDiscountGiven = x.TotalDiscountGiven,
            RevenueGenerated = x.RevenueGenerated,
            RoiPercentage = x.TotalDiscountGiven == 0 ? 0m
                : Math.Round((x.RevenueGenerated - x.TotalDiscountGiven) / x.TotalDiscountGiven * 100, 2)
          })
          .ToList();

      return new PromotionRoiResponse
      {
        StartDate = startDate.ToString("yyyy-MM-dd"),
        EndDate = endDate.ToString("yyyy-MM-dd"),
        TotalPromotions = items.Count,
        Items = items
      };
    }

    public async Task<RevenueDetailResponse> GetRevenueDetailReportAsync(DateTime startDate, DateTime endDate, string? paymentMethod)
    {
      // PaidAt is stored as UTC; shift VN date boundaries to UTC before comparing, same as
      // GetPromotionRoiReportAsync above.
      var rangeStart = startDate.Date.AddHours(-7);
      var rangeEnd = endDate.Date.AddDays(1).AddHours(-7);

      PaymentMethod? methodFilter = null;
      if (!string.IsNullOrWhiteSpace(paymentMethod) && !paymentMethod.Equals("all", StringComparison.OrdinalIgnoreCase))
      {
        if (!Enum.TryParse<PaymentMethod>(paymentMethod, true, out var parsed))
          throw new ArgumentException("Phương thức thanh toán không hợp lệ.");
        methodFilter = parsed;
      }

      var query =
          from t in _context.Transactions
          join b in _context.Bookings on t.BookingID equals b.BookingID
          where b.Status == BookingStatus.Completed
             && t.Status == TransactionStatus.Paid
             && t.PaidAt != null
             && t.PaidAt >= rangeStart
             && t.PaidAt < rangeEnd
          select new { Transaction = t, Booking = b };

      if (methodFilter.HasValue)
      {
        query = query.Where(x => x.Transaction.PaymentMethod == methodFilter.Value);
      }

      var rows = await query.ToListAsync();

      var customerNames = await _context.Customers.ToDictionaryAsync(c => c.CustomerID, c => c.FullName);
      var serviceNames = await _context.Services.ToDictionaryAsync(s => s.ServiceID, s => s.ServiceName);
      var promotions = await _context.Promotions.ToDictionaryAsync(p => p.PromotionID, p => new { p.Title, p.PromoCode });
      var rewardNames = await _context.RewardsCatalog.ToDictionaryAsync(r => r.RewardID, r => r.RewardName);

      var transactions = rows
          .OrderByDescending(x => x.Transaction.PaidAt)
          .Select(x =>
          {
            string? promotionApplied = null;
            if (x.Booking.PromotionID.HasValue && promotions.TryGetValue(x.Booking.PromotionID.Value, out var promo))
            {
              promotionApplied = string.IsNullOrWhiteSpace(promo.PromoCode) ? promo.Title : $"{promo.Title} ({promo.PromoCode})";
            }
            else if (x.Booking.RewardID.HasValue && rewardNames.TryGetValue(x.Booking.RewardID.Value, out var rewardName))
            {
              promotionApplied = rewardName;
            }

            return new RevenueTransactionItemDto
            {
              InvoiceCode = $"HD{x.Transaction.TransactionID:D5}",
              CustomerName = customerNames.TryGetValue(x.Booking.CustomerID, out var name) && !string.IsNullOrWhiteSpace(name)
                  ? name
                  : (string.IsNullOrWhiteSpace(x.Booking.Phone) ? "Khách vãng lai" : x.Booking.Phone),
              ServiceName = serviceNames.TryGetValue(x.Booking.ServiceID, out var svcName) ? svcName : "Dịch vụ không xác định",
              PaymentMethod = x.Transaction.PaymentMethod.ToString(),
              PromotionApplied = promotionApplied,
              DiscountAmount = x.Booking.DiscountApplied,
              Amount = x.Transaction.Amount,
              PaidAt = x.Transaction.PaidAt!.Value
            };
          })
          .ToList();

      return new RevenueDetailResponse
      {
        StartDate = startDate.ToString("yyyy-MM-dd"),
        EndDate = endDate.ToString("yyyy-MM-dd"),
        PaymentMethodFilter = methodFilter?.ToString() ?? "All",
        GrossRevenue = rows.Sum(x => x.Booking.BaseAmount),
        TotalDiscount = rows.Sum(x => x.Booking.DiscountApplied),
        NetRevenue = rows.Sum(x => x.Transaction.Amount),
        CashRevenue = rows.Where(x => x.Transaction.PaymentMethod == PaymentMethod.Cash).Sum(x => x.Transaction.Amount),
        TransferRevenue = rows.Where(x => x.Transaction.PaymentMethod == PaymentMethod.Transfer).Sum(x => x.Transaction.Amount),
        Transactions = transactions
      };
    }

  }
}
