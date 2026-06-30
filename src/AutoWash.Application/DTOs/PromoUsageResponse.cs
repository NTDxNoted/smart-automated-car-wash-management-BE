using System;

namespace AutoWash.Application.DTOs
{
    public class PromoUsageResponse
    {
        public int BookingId { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime UsedAt { get; set; }
        public decimal DiscountAmountActual { get; set; }
    }
}
