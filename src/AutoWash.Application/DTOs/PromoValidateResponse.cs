namespace AutoWash.Application.DTOs
{
    public class PromoValidateResponse
    {
        public int PromotionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string PromoCode { get; set; } = string.Empty;
        public string DiscountType { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public decimal MinOrderValue { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public bool IsValid { get; set; }
        public string? MinTierRequired { get; set; }
    }
}
