using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/rewards")]
    public class AdminRewardController : ControllerBase
    {
        private readonly IRewardService _rewardService;

        public AdminRewardController(IRewardService rewardService)
        {
            _rewardService = rewardService;
        }

        // POST /api/admin/rewards — tạo reward mới
        [HttpPost]
        public async Task<IActionResult> CreateReward([FromBody] CreateRewardRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _rewardService.CreateRewardAsync(request);
            return StatusCode(201, result);
        }

        // PUT /api/admin/rewards/{id} — cập nhật reward
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateReward(int id, [FromBody] CreateRewardRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var updated = await _rewardService.UpdateRewardAsync(id, request);
            if (!updated) return NotFound(new { message = "Reward not found" });

            return NoContent();
        }

        // PATCH /api/admin/rewards/{id}/toggle — toggle IsActive
        [HttpPatch("{id:int}/toggle")]
        public async Task<IActionResult> ToggleRewardStatus(int id)
        {
            var updated = await _rewardService.ToggleRewardStatusAsync(id);
            if (!updated) return NotFound(new { message = "Reward not found" });

            return Ok(new { message = "Reward status toggled successfully" });
        }
    }
}