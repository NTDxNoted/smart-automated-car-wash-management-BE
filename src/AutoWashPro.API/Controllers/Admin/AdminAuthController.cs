using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;
using AutoWashPro.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AutoWashPro.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin")]
    public class AdminAuthController : ControllerBase
    {
        private readonly IAdminAuthService _adminAuthService;

        public AdminAuthController(IAdminAuthService adminAuthService)
        {
            _adminAuthService = adminAuthService;
        }

        // 1. POST /api/admin/auth/login
        [HttpPost("auth/login")]
        [ProducesResponseType(typeof(AdminLoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] AdminLoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        error = "VALIDATION_FAILED",
                        message = "Dữ liệu không hợp lệ"
                    });
                }

                var response = await _adminAuthService.LoginAsync(request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                // In unified schema, any authorization failure (wrong role, wrong password, locked account, etc.) 
                // in login must return 401 Unauthorized with standard error format
                return StatusCode(StatusCodes.Status401Unauthorized, new
                {
                    error = "INVALID_CREDENTIALS",
                    message = "Số điện thoại hoặc mật khẩu không đúng"
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    error = "VALIDATION_FAILED",
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    error = "INTERNAL_SERVER_ERROR",
                    message = ex.Message
                });
            }
        }

        // 2. GET /api/admin/profile
        [HttpGet("profile")]
        [AuthorizeAdmin] // Protected with Admin privilege filter
        [ProducesResponseType(typeof(AdminProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                            ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

                if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out int adminId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new
                    {
                        error = "FORBIDDEN",
                        message = "Bạn không có quyền truy cập vào tài nguyên này"
                    });
                }

                var profile = await _adminAuthService.GetProfileAsync(adminId);
                return Ok(profile);
            }
            catch (KeyNotFoundException)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    error = "FORBIDDEN",
                    message = "Bạn không có quyền truy cập vào tài nguyên này"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    error = "INTERNAL_SERVER_ERROR",
                    message = ex.Message
                });
            }
        }
    }
}
