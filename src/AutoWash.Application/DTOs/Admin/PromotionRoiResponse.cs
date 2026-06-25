namespace AutoWash.Application.DTOs.Admin
{
  public class PromotionRoiResponse
  {
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public int TotalPromotions { get; set; }
    public List<PromotionRoiItemDto> Items { get; set; } = new();
  }

  public class PromotionRoiItemDto
  {
    public int PromotionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string PromoCode { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public decimal TotalDiscountGiven { get; set; }
    public decimal RevenueGenerated { get; set; }
    public decimal RoiPercentage { get; set; }
  }
}
