using System;

namespace AutoWash.Application.DTOs
{
    public class BookingResponseDto
    {
        public int BookingId { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public DateTime ScheduledTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal FinalAmount { get; set; }
        public int PointsEarned { get; set; }
    }
}