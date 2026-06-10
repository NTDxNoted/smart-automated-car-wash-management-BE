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
        public DateTime CreatedAt { get; set; }
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
}
