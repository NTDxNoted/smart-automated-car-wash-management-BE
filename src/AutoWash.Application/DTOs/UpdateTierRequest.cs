using System.ComponentModel.DataAnnotations;

namespace AutoWash.Application.DTOs
{
    public class UpdateTierRequest
    {
        [Range(0, double.MaxValue, ErrorMessage = "MinSpending phải >= 0.")]
        public decimal? MinSpending { get; set; }

        [Range(0, 100, ErrorMessage = "DiscountRate phải từ 0 đến 100.")]
        public decimal? DiscountRate { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "BookingWindowDays phải >= 1.")]
        public int? BookingWindowDays { get; set; }
    }
}
