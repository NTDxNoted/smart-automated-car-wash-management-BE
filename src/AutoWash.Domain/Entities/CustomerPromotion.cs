using System;

namespace AutoWash.Domain.Entities
{
  public class CustomerPromotion
  {
    public int ID { get; set; }
    public int CustomerID { get; set; }
    public int PromotionID { get; set; }
    public int BookingID { get; set; }
    public DateTime UsedAt { get; set; } = DateTime.UtcNow;
    public decimal DiscountAmountActual { get; set; }
  }
}
