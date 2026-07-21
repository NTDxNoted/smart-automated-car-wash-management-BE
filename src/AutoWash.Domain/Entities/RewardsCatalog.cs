using System;

namespace AutoWash.Domain.Entities
{
    public class RewardsCatalog
    {
        public int RewardID { get; set; }
        public string RewardName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int PointsRequired { get; set; }
        public decimal DiscountAmount { get; set; }
        public string DiscountType { get; set; } = "Fixed_Amount"; // "Fixed_Amount" | "Percentage"
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
