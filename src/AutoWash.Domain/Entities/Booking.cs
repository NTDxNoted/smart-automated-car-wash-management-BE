using System;
using AutoWash.Domain.Enums;

namespace AutoWash.Domain.Entities
{
    public class Booking
    {
        public int BookingID { get; set; }
        public int CustomerID { get; set; }
        public string Phone { get; set; } = string.Empty;
        public int VehicleID { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public int ServiceID { get; set; }
        public int? RewardID { get; set; }
        public int? PromotionID { get; set; }
        public DateTime ScheduledTime { get; set; }
        public DateTime? CheckInTime { get; set; }

        // Sử dụng Enum thay cho string để dễ quản lý logic
        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        public decimal BaseAmount { get; set; }
        public decimal DiscountApplied { get; set; }
        public decimal FinalAmount { get; set; }
        public int PointsEarned { get; set; }
        public int PointsRedeemed { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        // Navigation Properties (Tùy chọn, dùng để EF Core tự join bảng)
        // public virtual Customer Customer { get; set; }
    }
}