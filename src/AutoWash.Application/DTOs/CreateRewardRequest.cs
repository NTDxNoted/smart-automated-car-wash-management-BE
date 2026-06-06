namespace AutoWash.Application.DTOs
{
    public class CreateRewardRequest
    {
        public string RewardName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int PointsRequired { get; set; }
        public decimal DiscountAmount { get; set; }
    }
}
