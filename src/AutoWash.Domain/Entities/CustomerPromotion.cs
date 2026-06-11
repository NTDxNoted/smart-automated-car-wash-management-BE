using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoWash.Domain.Entities
{
    [Table("customerpromotion")]
    public class CustomerPromotion
    {
        public int ID { get; set; }
        public int CustomerID { get; set; }
        public int PromotionID { get; set; }
        public int BookingID { get; set; }
        public DateTime UsedAt { get; set; } = DateTime.UtcNow;
        public decimal DiscountAmountActual { get; set; }

        public Customer? Customer { get; set; }
        public Promotion? Promotion { get; set; }
        public Booking? Booking { get; set; }
    }
}
