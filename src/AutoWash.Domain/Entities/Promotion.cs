using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoWash.Domain.Entities
{
  [Table("promotion")]
  public class Promotion
  {
    public int PromotionID { get; set; }
    public string Title { get; set; } = string.Empty;
    public string PromoCode { get; set; } = string.Empty;
    public int? MinTierID { get; set; }
    public string DiscountType { get; set; } = "Fixed_Amount";
    public decimal DiscountValue { get; set; }
    public int? MaxUsage { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal MinOrderValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }

    public Tier? MinTier { get; set; }
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
  }
}
