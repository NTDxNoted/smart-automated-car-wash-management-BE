using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;

namespace AutoWashPro.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/rewards")]
    public class AdminRewardController : ControllerBase
    {
        private readonly IRewardService _rewardService;

        public AdminRewardController(IRewardService rewardService)
        {
            _rewardService = rewardService;
        }

        private IActionResult? CheckAdminAccess()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized(new { message = "UNAUTHORIZED: Cần đăng nhập." });

            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.Equals(role,"Admin",System.StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, new { message = "FORBIDDEN: Chỉ Admin mới được thực hiện." });

            return null;
        }

        // POST /api/admin/rewards
        [HttpPost]
        public async Task<IActionResult> CreateReward([FromBody] CreateRewardRequest request)
        {
            var authError = CheckAdminAccess();
            if (authError != null) return authError;

            try
            {
                var reward = await _rewardService.CreateRewardAsync(request);
                return Created($"/api/rewards/{reward.RewardId}", reward);
            }
            catch (Exception ex) when (ex.Message.StartsWith("INVALID_REQUEST"))
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT /api/admin/rewards/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReward(int id, [FromBody] UpdateRewardRequest request)
        {
            var authError = CheckAdminAccess();
            if (authError != null) return authError;

            try
            {
                var reward = await _rewardService.UpdateRewardAsync(id, request);
                return Ok(reward);
            }
            catch (Exception ex) when (ex.Message.StartsWith("NOT_FOUND"))
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // PATCH /api/admin/rewards/{id}/toggle
        [HttpPatch("{id}/toggle")]
        public async Task<IActionResult> ToggleRewardStatus(int id)
        {
            var authError = CheckAdminAccess();
            if (authError != null) return authError;

            try
            {
                var reward = await _rewardService.ToggleRewardStatusAsync(id);
                return Ok(reward);
            }
            catch (Exception ex) when (ex.Message.StartsWith("NOT_FOUND"))
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}

