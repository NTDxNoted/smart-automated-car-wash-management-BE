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

    private static string DetermineTier(int totalPoints)
    {
      if (totalPoints >= 5000) return "Platinum";
      if (totalPoints >= 2000) return "Gold";
      if (totalPoints >= 500) return "Silver";
      return "Member";
    }
  }
}
