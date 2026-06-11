namespace AutoWash.Domain.Entities
{
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
  }
}
