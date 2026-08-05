using System.Collections.Generic;

namespace AutoWash.Application.Common
{
    // Shared by AdminBookingService and ReportService so invoice code / promotion label
    // formatting never drifts apart between the booking list and the revenue report.
    public static class BookingDisplayHelper
    {
        public static string FormatInvoiceCode(int transactionId) => $"HD{transactionId:D5}";

        public static string? GetPromotionApplied(
            int? promotionId,
            int? rewardId,
            Dictionary<int, (string Title, string PromoCode)> promotions,
            Dictionary<int, string> rewardNames)
        {
            if (promotionId.HasValue && promotions.TryGetValue(promotionId.Value, out var promo))
            {
                return string.IsNullOrWhiteSpace(promo.PromoCode) ? promo.Title : $"{promo.Title} ({promo.PromoCode})";
            }
            if (rewardId.HasValue && rewardNames.TryGetValue(rewardId.Value, out var rewardName))
            {
                return rewardName;
            }
            return null;
        }
    }
}
