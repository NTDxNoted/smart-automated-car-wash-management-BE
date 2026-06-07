using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;

namespace AutoWashPro.API.Controllers
{
    [ApiController]
    [Route("api/profile")]
    public class ProfileController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public ProfileController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        // BR-07: Chặn Guest, BR-11: chỉ trả data của chính customer trong JWT
        private IActionResult? CheckMemberAccess(out int customerId)
        {
            customerId = 0;
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (claim == null)
                return Unauthorized(new { message = "UNAUTHORIZED: Cần đăng nhập." });
            customerId = int.Parse(claim);
            return null;
        }

        // GET /api/profile
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var authError = CheckMemberAccess(out int customerId);
            if (authError != null) return authError;

            try
            {
                var profile = await _customerService.GetProfileAsync(customerId);
                return Ok(profile);
            }
            catch (Exception ex) when (ex.Message.StartsWith("NOT_FOUND"))
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // PUT /api/profile
        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var authError = CheckMemberAccess(out int customerId);
            if (authError != null) return authError;

            try
            {
                var profile = await _customerService.UpdateProfileAsync(customerId, request);
                return Ok(profile);
            }
            catch (Exception ex) when (ex.Message.StartsWith("NOT_FOUND"))
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
