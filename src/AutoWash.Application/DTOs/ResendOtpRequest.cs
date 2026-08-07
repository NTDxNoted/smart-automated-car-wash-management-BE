using System.ComponentModel.DataAnnotations;

namespace AutoWash.Application.DTOs
{
  public class ResendOtpRequest
  {
    [Required(ErrorMessage = "Email không được để trống")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;
  }
}
