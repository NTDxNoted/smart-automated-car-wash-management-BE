using System.Collections.Generic;

namespace AutoWash.Application.DTOs
{
    public class PromoDetailResponse
    {
        public int PromotionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string PromoCode { get; set; } = string.Empty;

        // Statistic window actually applied (yyyy-MM-dd) — capped to the last 365 days,
        // and further clamped to the promotion's StartDate if it is newer than that.
        public string RangeStart { get; set; } = string.Empty;
        public string RangeEnd { get; set; } = string.Empty;

        public int TotalUsageCount { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public decimal TotalRevenueGenerated { get; set; }
        public int UniqueCustomerCount { get; set; }
        public decimal EffectivenessPercentage { get; set; }

        public List<PromoMonthlyStatDto> MonthlyStats { get; set; } = new();
        public List<PromoUsageResponse> Usages { get; set; } = new();
    }

    public class PromoMonthlyStatDto
    {
        // "yyyy-MM"
        public string Month { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal Discount { get; set; }
    }
}
