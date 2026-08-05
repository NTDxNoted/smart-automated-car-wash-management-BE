using AutoWash.Application.DTOs.Admin;
using System.Collections.Generic;
using System;

namespace AutoWash.Application.Interfaces
{
  public interface IReportService
  {
    Task<OverviewReportResponse> GetOverviewReportAsync(string filterType, DateTime? startDate, DateTime? endDate);
    Task<IReadOnlyList<PopularServiceResponse>> GetPopularServicesReportAsync(DateTime? startDate, DateTime? endDate);
    Task<IReadOnlyList<RfmReportResponse>> GetRfmReportAsync();
    Task<IReadOnlyList<TierDistributionResponse>> GetTierDistributionAsync();
    Task<LoyaltyStatsResponse> GetLoyaltyStatsAsync();
    Task<PeakOccupancyResponse> GetPeakOccupancyReportAsync(DateTime startDate, DateTime endDate);
    Task<PromotionRoiResponse> GetPromotionRoiReportAsync(DateTime startDate, DateTime endDate);
    Task<RevenueDetailResponse> GetRevenueDetailReportAsync(DateTime startDate, DateTime endDate, string? paymentMethod);
    Task<CompletionRateDetailResponse> GetCompletionRateDetailAsync(string? filterType, DateTime? startDate, DateTime? endDate, int? serviceId, string? groupBy);
    Task<UnfinishedBookingsPageDto> GetUnfinishedBookingsAsync(string? filterType, DateTime? startDate, DateTime? endDate, int? serviceId, int page, int pageSize);
    Task<byte[]> ExportUnfinishedBookingsAsync(string? filterType, DateTime? startDate, DateTime? endDate, int? serviceId);
  }
}
