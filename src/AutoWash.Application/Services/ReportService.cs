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

    public async Task<OverviewReportResponse> GetOverviewReportAsync()
    {
      var now = DateTime.UtcNow;
      var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

      var bookings = await _context.Bookings
          .Where(b => b.CreatedAt >= startOfMonth && b.CreatedAt < startOfMonth.AddMonths(1))
          .ToListAsync();

      var totalBookings = bookings.Count;
      var completedBookings = bookings.Count(b => b.Status == BookingStatus.Completed);
      var failedBookings = bookings.Count(b => b.Status == BookingStatus.Failed);
      var noShowBookings = bookings.Count(b => b.Status == BookingStatus.NoShow);
      var cancelledBookings = bookings.Count(b => b.Status == BookingStatus.Cancelled);
      var totalRevenue = bookings.Where(b => b.Status == BookingStatus.Completed).Sum(b => b.FinalAmount);

      return new OverviewReportResponse
      {
        Period = now.ToString("yyyy-MM"),
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
      var customers = await _context.Customers
          .Include(c => c.LoyaltyAccount)
          .ToListAsync();

      var totalCustomers = customers.Count;
      var distribution = customers
          .GroupBy(c => DetermineTier(c.LoyaltyAccount?.TotalPoints ?? 0))
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

    private static string DetermineTier(int totalPoints)
    {
      if (totalPoints >= 5000) return "Platinum";
      if (totalPoints >= 2000) return "Gold";
      if (totalPoints >= 500) return "Silver";
      return "Member";
    }
  }
}
