using System.ComponentModel.DataAnnotations;

namespace AutoWash.Application.DTOs
{
  public class VerifyLoginOtpRequest
  {
    // Định danh theo Phone (không phải Email) — sau bước login client chỉ có
    // MaskedEmail (vd. "te**@example.com") để hiển thị, không có email thật để gửi lại.
    [Required(ErrorMessage = "Số điện thoại không được để trống")]
    [RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "Số điện thoại không hợp lệ")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mã OTP không được để trống")]
    public string Code { get; set; } = string.Empty;
  }
}
