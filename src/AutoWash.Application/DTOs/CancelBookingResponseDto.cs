namespace AutoWash.Application.DTOs
{
    public class CancelBookingResponseDto
    {
        public int BookingId { get; set; }
        public string Status { get; set; } = string.Empty;
        public int PointsRefunded { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}