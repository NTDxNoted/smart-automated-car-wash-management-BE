using AutoWash.Application.DTOs.Admin;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AutoWash.Application.Services
{
  public class ReportService : IReportService
  {
    private readonly IApplicationDbContext _context;

    public ReportService(IApplicationDbContext context)
    {
      _context = context;
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
              return new PopularServiceResponse
              {
                  ServiceId = g.Key,
                  ServiceName = service?.ServiceName ?? "Dịch vụ không xác định",
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
      const int maxParallelSlots = 1;
      var eligibleStatuses = new[] { BookingStatus.Completed, BookingStatus.Pending };

      var rangeStart = startDate.Date;
      var rangeEnd = endDate.Date.AddDays(1);
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

  }
}
