namespace AutoWash.Application.DTOs
{
    public class UpdateTierRequest
    {
        public decimal? MinSpending { get; set; }
        public decimal? DiscountRate { get; set; }
        public int? BookingWindowDays { get; set; }
    }
}
