using AutoWash.Application.DTOs.Admin;

namespace AutoWash.Application.Interfaces
{
  public interface IReportService
  {
    Task<OverviewReportResponse> GetOverviewReportAsync();
    Task<IReadOnlyList<RfmReportResponse>> GetRfmReportAsync();
    Task<IReadOnlyList<TierDistributionResponse>> GetTierDistributionAsync();
    Task<LoyaltyStatsResponse> GetLoyaltyStatsAsync();
    Task<PeakOccupancyResponse> GetPeakOccupancyReportAsync(DateTime startDate, DateTime endDate);
  }
}
