using System;

namespace AutoWash.Domain.Entities
{
  public class CustomerPromotion
  {
    public int CustomerPromotionID { get; set; }
    public int CustomerID { get; set; }
    public int PromotionID { get; set; }
    public int BookingID { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  }
}
