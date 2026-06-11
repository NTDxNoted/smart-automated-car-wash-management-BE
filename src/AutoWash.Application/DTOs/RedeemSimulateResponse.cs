namespace AutoWash.Application.DTOs
{
    public class RedeemSimulateResponse
    {
        public int RewardId { get; set; }
        public string RewardName { get; set; } = string.Empty;
        public int PointsRequired { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal MaxAllowed { get; set; }
        public decimal DiscountApplied { get; set; }
        public decimal FinalAmount { get; set; }
        public bool IsValid { get; set; }
    }
}
