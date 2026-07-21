namespace AutoWash.Application.DTOs
{
    public class UpdateRewardRequest
    {
        public string? RewardName { get; set; }
        public string? Description { get; set; }
        public int? PointsRequired { get; set; }
        public decimal? DiscountAmount { get; set; }
        public string? DiscountType { get; set; }
    }
}
