using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AutoWash.Application.Interfaces;

namespace AutoWashPro.API.Controllers
{
    [ApiController]
    [Route("api/rewards")]
    public class RewardsController : ControllerBase
    {
        private readonly IRewardService _rewardService;

        public RewardsController(IRewardService rewardService)
        {
            _rewardService = rewardService;
        }

        // GET /api/rewards — cần auth Member
        [HttpGet]
        public async Task<IActionResult> GetActiveRewards()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized(new { message = "UNAUTHORIZED: Cần đăng nhập để xem rewards." });

            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role != "Member")
                return StatusCode(403, new { message = "FORBIDDEN: Chỉ Member mới được xem rewards." });

            var rewards = await _rewardService.GetActiveRewardsAsync();
            return Ok(new { data = rewards });
        }
    }
}
