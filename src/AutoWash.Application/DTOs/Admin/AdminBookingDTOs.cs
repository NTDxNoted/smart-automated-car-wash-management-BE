using System;

namespace AutoWash.Application.DTOs.Admin
{
    public class AdminBookingListResponse
    {
        public int BookingID { get; set; }
        public int CustomerID { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int VehicleID { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public int ServiceID { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public DateTime ScheduledTime { get; set; }
        public DateTime? CheckInTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal DiscountApplied { get; set; }
        public decimal FinalAmount { get; set; }
        public int PointsEarned { get; set; }
        public string? PaymentStatus { get; set; }
        public string? PaymentMethod { get; set; }
        public DateTime? PaymentAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsWalkIn { get; set; }
    }

    public class UpdateBookingStatusRequest
    {
        public string NewStatus { get; set; } = string.Empty;
    }

    public class UpdateLicensePlateRequest
    {
        public string NewLicensePlate { get; set; } = string.Empty;
    }

    public class EmergencyStopRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class CreateWalkInBookingRequest
    {
        public string CustomerName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Plate { get; set; } = string.Empty;
        public int ServiceId { get; set; }
        public string BookingDate { get; set; } = string.Empty;
        public string BookingTime { get; set; } = string.Empty;
    }
}
