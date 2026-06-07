using System.ComponentModel.DataAnnotations;

namespace AutoWash.Application.DTOs
{
  public class LoginRequest
  {
    [Required(ErrorMessage = "Số điện thoại không được để trống")]
    [RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "Số điện thoại không hợp lệ")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống")]
    public string Password { get; set; } = string.Empty;
  }
}
