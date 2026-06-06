using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AutoWash.Application.DTOs;
using AutoWash.Application.Exceptions;
using AutoWash.Application.Interfaces;

namespace AutoWashPro.API.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  public class AuthController : ControllerBase
  {
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
      _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
      try
      {
        var response = await _authService.RegisterAsync(request);
        return Created(string.Empty, response);
      }
      catch (AuthException ex) when (ex.ErrorCode == "PHONE_ALREADY_EXISTS")
      {
        return BadRequest(new { error = ex.ErrorCode, message = ex.Message });
      }
      catch (AuthException ex)
      {
        return BadRequest(new { error = ex.ErrorCode, message = ex.Message });
      }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
      try
      {
        var response = await _authService.LoginAsync(request);
        return Ok(response);
      }
      catch (AuthException ex) when (ex.ErrorCode == "ACCOUNT_LOCKED")
      {
        return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.ErrorCode, message = ex.Message });
      }
      catch (AuthException ex)
      {
        return BadRequest(new { error = ex.ErrorCode, message = ex.Message });
      }
    }
  }
}
