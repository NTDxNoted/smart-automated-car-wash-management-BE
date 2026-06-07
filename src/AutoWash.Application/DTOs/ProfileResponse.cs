using System;

namespace AutoWash.Application.DTOs
{
    public class ProfileResponse
    {
        public int CustomerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Tier { get; set; } = string.Empty;
        public decimal TotalSpending { get; set; }
        public int LoyaltyPoints { get; set; }
        public DateTime? LastVisit { get; set; }
        public DateTime? SuspendedUntil { get; set; }
    }
}
