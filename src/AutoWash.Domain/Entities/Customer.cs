using System;

namespace AutoWash.Domain.Entities
{
    public class Customer
    {
        public int CustomerID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Tier { get; set; } = "Bronze";
        public decimal TotalSpending { get; set; }
        public DateTime? LastVisit { get; set; }
        public DateTime? SuspendedUntil { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
