using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoWash.Domain.Entities
{
    [Table("customernotification")]
    public class CustomerNotification
    {
        public int ID { get; set; }
        public int CustomerID { get; set; }
        public int? PromotionID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string PromoCode { get; set; } = string.Empty;
        public decimal? DiscountValue { get; set; }
        public string? DiscountType { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }

        public Customer? Customer { get; set; }
        public Promotion? Promotion { get; set; }
    }
}
