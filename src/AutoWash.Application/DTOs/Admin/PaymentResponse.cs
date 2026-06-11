using System;

namespace AutoWash.Application.DTOs.Admin
{
    public class PaymentResponse
    {
        public int TransactionId { get; set; }
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime? PaidAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string BookingStatus { get; set; } = string.Empty;
        public PaymentLoyaltyInfo Loyalty { get; set; } = new();
    }

    public class PaymentLoyaltyInfo
    {
        public int PointsEarned { get; set; }
        public int TotalPointsAfter { get; set; }
        public string TierAfter { get; set; } = string.Empty;
    }
}
