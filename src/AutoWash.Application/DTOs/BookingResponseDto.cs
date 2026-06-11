using System;

namespace AutoWash.Application.DTOs
{
    public class BookingResponseDto
    {
        public int BookingId { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public ServiceResponse Service { get; set; } = new();
        public DateTime ScheduledTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public InvoiceResponseDto Invoice { get; set; } = new();
        public decimal FinalAmount { get; set; }
        public int PointsEarned { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}