using System;
using System.Collections.Generic; // Cần thêm dòng này để dùng ICollection

namespace AutoWash.Domain.Entities
{
    public class LoyaltyAccount
    {
        public int LoyaltyID { get; set; }
        public int CustomerID { get; set; }
        public int TotalPoints { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public ICollection<PointTransaction> PointTransactions { get; set; } = new List<PointTransaction>();
    }
}