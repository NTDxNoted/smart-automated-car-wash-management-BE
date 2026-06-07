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

        // SỬA DÒNG NÀY: Thay 'object' thành 'ICollection<Booking>'
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}