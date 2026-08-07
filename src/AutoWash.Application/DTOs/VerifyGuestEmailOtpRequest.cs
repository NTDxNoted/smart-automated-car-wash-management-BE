using System.ComponentModel.DataAnnotations;

namespace AutoWash.Application.DTOs
{
  public class VerifyGuestEmailOtpRequest
  {
    [Required(ErrorMessage = "Email không được để trống")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mã OTP không được để trống")]
    public string Code { get; set; } = string.Empty;
  }
}
