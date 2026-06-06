namespace AutoWash.Domain.Entities
{
  public class Promotion
  {
    public int PromotionID { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public decimal DiscountRate { get; set; }
    public bool IsActive { get; set; } = true;
  }
}
