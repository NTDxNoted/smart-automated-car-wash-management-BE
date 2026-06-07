using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
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
          Error = ex.GetType().Name,
          Message = ex.InnerException?.Message ?? ex.Message
        });
      }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
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
          Error = ex.GetType().Name,
          Message = ex.InnerException?.Message ?? ex.Message
        });
      }
    }
  }
}