using System.ComponentModel.DataAnnotations;

namespace AutoWash.Application.DTOs.Admin
{
    public class RecordPaymentRequest
    {
        [Required(ErrorMessage = "Phương thức thanh toán là bắt buộc.")]
        public string PaymentMethod { get; set; } = string.Empty;

        public bool? Confirmed { get; set; }
    }
}
