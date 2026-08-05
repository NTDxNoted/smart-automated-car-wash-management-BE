using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema; // Thêm dòng này

namespace AutoWash.Domain.Entities
{
    // Báo cho EF Core biết phải map đúng vào bảng "customer" (viết thường, số ít)
    [Table("customer")]
    public class Customer
    {
        public int CustomerID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = "MEMBER";
        [NotMapped]
        public string Tier { get; set; } = "1";
        public int TierID { get; set; } = 1;
        public string Password { get; set; } = string.Empty; // Thêm dòng này để khớp với DB
        public decimal TotalSpending { get; set; }
        public DateTime? LastVisit { get; set; }
        public bool IsLocked { get; set; }
        public DateTime? SuspendedUntil { get; set; }
        public string? ActiveSessionId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? AdminNotes { get; set; }

        public LoyaltyAccount? LoyaltyAccount { get; set; }
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
