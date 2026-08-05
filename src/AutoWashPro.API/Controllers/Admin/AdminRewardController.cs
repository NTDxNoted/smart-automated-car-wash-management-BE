using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;

namespace AutoWashPro.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/rewards")]
    [Authorize(Roles = "ADMIN")]
    public class AdminRewardController : ControllerBase
    {
        private readonly IRewardService _rewardService;

        public AdminRewardController(IRewardService rewardService)
        {
            _rewardService = rewardService;
        }

        // GET /api/admin/rewards
        [HttpGet]
        public async Task<IActionResult> GetAllRewards()
        {
            var rewards = await _rewardService.GetAllRewardsAsync();
            return Ok(rewards);
        }

        // POST /api/admin/rewards
        [HttpPost]
        public async Task<IActionResult> CreateReward([FromBody] CreateRewardRequest request)
        {
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

        // DELETE /api/admin/rewards/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReward(int id)
        {
            try
            {
                var reward = await _rewardService.DeleteRewardAsync(id);
                return Ok(reward);
            }
            catch (Exception ex) when (ex.Message.StartsWith("NOT_FOUND"))
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex) when (ex.Message.StartsWith("REWARD_IN_USE"))
            {
                return Conflict(new { message = ex.Message });
            }
        }
    }
}
