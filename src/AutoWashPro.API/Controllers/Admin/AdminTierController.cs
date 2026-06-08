using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;

namespace AutoWashPro.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/tiers")]
    public class AdminTierController : ControllerBase
    {
        private readonly ITierService _tierService;

        public AdminTierController(ITierService tierService)
        {
            _tierService = tierService;
        }

        private IActionResult? CheckAdminAccess()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized(new { message = "UNAUTHORIZED: Cần đăng nhập." });

            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role != "Admin")
                return StatusCode(403, new { message = "FORBIDDEN: Chỉ Admin mới được thực hiện." });

            return null;
        }

        // GET /api/admin/tiers
        [HttpGet]
        public async Task<IActionResult> GetAllTiers()
        {
            var authError = CheckAdminAccess();
            if (authError != null) return authError;

            var tiers = await _tierService.GetAllTiersAsync();
            return Ok(new { data = tiers });
        }

        // PUT /api/admin/tiers/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTier(int id, [FromBody] UpdateTierRequest request)
        {
            var authError = CheckAdminAccess();
            if (authError != null) return authError;

            try
            {
                var tier = await _tierService.UpdateTierAsync(id, request);
                return Ok(tier);
            }
            catch (Exception ex) when (ex.Message.StartsWith("NOT_FOUND"))
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
