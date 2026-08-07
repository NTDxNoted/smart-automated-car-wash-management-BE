using AutoWash.Application.Common;
using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AutoWashPro.API.Controllers
{
  [ApiController]
  [Route("api/auth")]
  public class AuthController : ControllerBase
  {
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
      _authService = authService;
      _logger = logger;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
      try
      {
        if (!ModelState.IsValid)
        {
          var errors = ModelState.Values.SelectMany(v => v.Errors);
          var errorMessage = string.Join("; ", errors.Select(e => e.ErrorMessage));

          return BadRequest(new ErrorResponse
          {
            Error = "VALIDATION_FAILED",
            Message = errorMessage
          });
        }

        var response = await _authService.RegisterAsync(request);

        return StatusCode(StatusCodes.Status201Created, response);
      }
      catch (InvalidOperationException ex) when (ex.Message == "PHONE_ALREADY_EXISTS")
      {
        _logger.LogWarning(ex, "Phone already registered: {Phone}", request.Phone);

        return BadRequest(new ErrorResponse
        {
          Error = "PHONE_ALREADY_EXISTS",
          Message = "Số điện thoại đã được đăng ký"
        });
      }
      catch (InvalidOperationException ex) when (ex.Message == "EMAIL_ALREADY_EXISTS")
      {
        _logger.LogWarning(ex, "Email already registered: {Email}", request.Email);

        return BadRequest(new ErrorResponse
        {
          Error = "EMAIL_ALREADY_EXISTS",
          Message = "Email đã được đăng ký"
        });
      }
      catch (ArgumentException ex)
      {
        _logger.LogWarning(ex, "Registration validation error");

        return BadRequest(new ErrorResponse
        {
          Error = "VALIDATION_FAILED",
          Message = ex.Message
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Registration error");

        return StatusCode(500, new ErrorResponse
        {
          Error = "INTERNAL_SERVER_ERROR",
          Message = "Đã có lỗi xảy ra, vui lòng thử lại sau."
        });
      }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
      try
      {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("nameid")?.Value
                    ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                    ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var customerId))
        {
          return Unauthorized(new ErrorResponse
          {
            Error = "SESSION_EXPIRED",
            Message = "Phiên đăng nhập không hợp lệ"
          });
        }

        await _authService.LogoutAsync(customerId);
        return Ok(new { message = "Đăng xuất thành công" });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Logout error");
        return StatusCode(500, new ErrorResponse
        {
          Error = "INTERNAL_SERVER_ERROR",
          Message = "Đã có lỗi xảy ra, vui lòng thử lại sau."
        });
      }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LoginOtpRequiredResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
      try
      {
        if (!ModelState.IsValid)
        {
          var errors = ModelState.Values.SelectMany(v => v.Errors);
          var errorMessage = string.Join("; ", errors.Select(e => e.ErrorMessage));

          return BadRequest(new ErrorResponse
          {
            Error = "VALIDATION_FAILED",
            Message = errorMessage
          });
        }

        var response = await _authService.LoginAsync(request);

        return Ok(response);
      }
      catch (TwoFactorRequiredException ex)
      {
        _logger.LogInformation("2FA OTP required for phone: {Phone}", request.Phone);

        return Ok(new LoginOtpRequiredResponse
        {
          RequiresOtp = true,
          MaskedEmail = ex.MaskedEmail
        });
      }
      catch (UnauthorizedAccessException ex) when (ex.Message == "INVALID_CREDENTIALS")
      {
        _logger.LogWarning(ex, "Invalid login attempt for phone: {Phone}", request.Phone);

        return BadRequest(new ErrorResponse
        {
          Error = "INVALID_CREDENTIALS",
          Message = "Số điện thoại hoặc mật khẩu không đúng"
        });
      }
      catch (InvalidOperationException ex) when (ex.Message == "ACCOUNT_LOCKED")
      {
        _logger.LogWarning(ex, "Login attempt for locked account: {Phone}", request.Phone);

        return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse
        {
          Error = "ACCOUNT_LOCKED",
          Message = "Tài khoản đã bị khóa, vui lòng liên hệ Admin"
        });
      }
      catch (InvalidOperationException ex) when (ex.Message == "EMAIL_NOT_VERIFIED")
      {
        _logger.LogWarning(ex, "Login attempt for unverified email, phone: {Phone}", request.Phone);

        return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse
        {
          Error = "EMAIL_NOT_VERIFIED",
          Message = "Email chưa được xác thực, vui lòng kiểm tra hộp thư"
        });
      }
      catch (InvalidOperationException ex) when (ex.Message == "OTP_SEND_FAILED")
      {
        _logger.LogError(ex, "Failed to send 2FA OTP email for phone: {Phone}", request.Phone);

        return StatusCode(500, new ErrorResponse
        {
          Error = "OTP_SEND_FAILED",
          Message = "Không thể gửi mã OTP lúc này, vui lòng thử lại sau"
        });
      }
      catch (ArgumentException ex)
      {
        _logger.LogWarning(ex, "Login validation error");

        return BadRequest(new ErrorResponse
        {
          Error = "VALIDATION_FAILED",
          Message = ex.Message
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Login error");

        return StatusCode(500, new ErrorResponse
        {
          Error = "INTERNAL_SERVER_ERROR",
          Message = "Đã có lỗi xảy ra, vui lòng thử lại sau."
        });
      }
    }

    [HttpPost("verify-login-otp")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyLoginOtp([FromBody] VerifyLoginOtpRequest request)
    {
      try
      {
        if (!ModelState.IsValid)
          return BadRequest(BuildValidationError());

        var response = await _authService.VerifyLoginOtpAsync(request);
        return Ok(response);
      }
      catch (InvalidOperationException ex)
      {
        return BadRequest(MapOtpError(ex.Message));
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Verify login OTP error");
        return StatusCode(500, new ErrorResponse { Error = "INTERNAL_SERVER_ERROR", Message = "Đã có lỗi xảy ra, vui lòng thử lại sau." });
      }
    }

    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
      try
      {
        if (!ModelState.IsValid)
          return BadRequest(BuildValidationError());

        await _authService.VerifyEmailAsync(request);
        return Ok(new { message = "Xác thực email thành công" });
      }
      catch (InvalidOperationException ex)
      {
        return BadRequest(MapOtpError(ex.Message));
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Verify email error");
        return StatusCode(500, new ErrorResponse { Error = "INTERNAL_SERVER_ERROR", Message = "Đã có lỗi xảy ra, vui lòng thử lại sau." });
      }
    }

    [HttpPost("resend-verification-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendVerificationEmail([FromBody] ResendOtpRequest request)
    {
      try
      {
        if (!ModelState.IsValid)
          return BadRequest(BuildValidationError());

        await _authService.ResendVerificationEmailAsync(request);
        return Ok(new { message = "Đã gửi lại mã xác thực" });
      }
      catch (InvalidOperationException ex)
      {
        return BadRequest(MapOtpError(ex.Message));
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Resend verification email error");
        return StatusCode(500, new ErrorResponse { Error = "INTERNAL_SERVER_ERROR", Message = "Đã có lỗi xảy ra, vui lòng thử lại sau." });
      }
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
      if (!ModelState.IsValid)
        return BadRequest(BuildValidationError());

      try
      {
        await _authService.ForgotPasswordAsync(request);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Forgot password error");
      }

      // Luôn trả về thông báo chung — không tiết lộ email có tồn tại trong hệ thống hay không.
      return Ok(new { message = "Nếu email tồn tại, mã OTP đã được gửi" });
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
      try
      {
        if (!ModelState.IsValid)
          return BadRequest(BuildValidationError());

        await _authService.ResetPasswordAsync(request);
        return Ok(new { message = "Đặt lại mật khẩu thành công" });
      }
      catch (ArgumentException ex)
      {
        return BadRequest(new ErrorResponse { Error = "VALIDATION_FAILED", Message = ex.Message });
      }
      catch (InvalidOperationException ex)
      {
        return BadRequest(MapOtpError(ex.Message));
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Reset password error");
        return StatusCode(500, new ErrorResponse { Error = "INTERNAL_SERVER_ERROR", Message = "Đã có lỗi xảy ra, vui lòng thử lại sau." });
      }
    }

    [HttpPost("2fa")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SetTwoFactor([FromBody] SetTwoFactorRequest request)
    {
      try
      {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var customerId))
        {
          return Unauthorized(new ErrorResponse { Error = "SESSION_EXPIRED", Message = "Phiên đăng nhập không hợp lệ" });
        }

        await _authService.SetTwoFactorEnabledAsync(customerId, request.Enable);
        return Ok(new { message = request.Enable ? "Đã bật xác thực 2 lớp" : "Đã tắt xác thực 2 lớp" });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Set 2FA error");
        return StatusCode(500, new ErrorResponse { Error = "INTERNAL_SERVER_ERROR", Message = "Đã có lỗi xảy ra, vui lòng thử lại sau." });
      }
    }

    private ErrorResponse BuildValidationError()
    {
      var errors = ModelState.Values.SelectMany(v => v.Errors);
      return new ErrorResponse
      {
        Error = "VALIDATION_FAILED",
        Message = string.Join("; ", errors.Select(e => e.ErrorMessage))
      };
    }

    private static ErrorResponse MapOtpError(string code)
    {
      var message = code switch
      {
        "OTP_NOT_FOUND" => "Không tìm thấy mã OTP, vui lòng yêu cầu gửi lại",
        "OTP_EXPIRED" => "Mã OTP đã hết hạn, vui lòng yêu cầu gửi lại",
        "OTP_LOCKED" => "Bạn đã nhập sai quá số lần cho phép, vui lòng yêu cầu gửi lại mã",
        "OTP_INVALID" => "Mã OTP không đúng",
        "OTP_COOLDOWN" => "Vui lòng đợi một chút trước khi yêu cầu gửi lại mã",
        "OTP_SEND_FAILED" => "Không thể gửi mã OTP lúc này, vui lòng thử lại sau",
        "CUSTOMER_NOT_FOUND" => "Không tìm thấy tài khoản",
        "EMAIL_ALREADY_VERIFIED" => "Email đã được xác thực trước đó",
        _ => "Đã có lỗi xảy ra, vui lòng thử lại sau."
      };

      return new ErrorResponse { Error = code, Message = message };
    }
  }
}
