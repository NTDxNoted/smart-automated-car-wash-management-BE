namespace AutoWash.Application.DTOs
{
  public class BookingInvoiceDto
  {
    public decimal BaseAmount { get; set; }
    public decimal TierDiscount { get; set; }
    public decimal RewardDiscount { get; set; }
    public decimal PromotionDiscount { get; set; }
    public decimal DiscountApplied { get; set; }
    public decimal FinalAmount { get; set; }
  }
}
