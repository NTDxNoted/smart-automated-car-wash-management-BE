namespace AutoWash.Domain.Entities
{
    public class Tier
    {
        public int TierID { get; set; }
        public string TierName { get; set; } = string.Empty;
        public decimal MinSpending { get; set; }
        public int BookingWindowDays { get; set; }
        public decimal DiscountRate { get; set; }
        public int PriorityScore { get; set; }
    }
}
