using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AutoWash.Application.Interfaces;

namespace AutoWashPro.API.Controllers
{
    [ApiController]
    [Route("api/loyalty")]
    public class LoyaltyController : ControllerBase
    {
        private readonly IPointService _pointService;

        public LoyaltyController(IPointService pointService)
        {
            _pointService = pointService;
        }

        // GET /api/loyalty — xem ví điểm + breakdown batch hết hạn
        [HttpGet]
        public async Task<IActionResult> GetWallet()
        {
            var customerId = GetCustomerId();
            if (customerId == null)
                return Unauthorized(new { error = "UNAUTHORIZED", message = "Cần đăng nhập để xem ví điểm." });

            var wallet = await _pointService.GetWalletAsync(customerId.Value);
            return Ok(wallet);
        }

        // GET /api/loyalty/history — lịch sử PointTransaction
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var customerId = GetCustomerId();
            if (customerId == null)
                return Unauthorized(new { error = "UNAUTHORIZED", message = "Cần đăng nhập để xem lịch sử điểm." });

            var history = await _pointService.GetHistoryAsync(customerId.Value);
            return Ok(history);
        }

        // GET /api/loyalty/simulate?rewardId=X&baseAmount=Y — preview giảm giá
        [HttpGet("simulate")]
        public async Task<IActionResult> Simulate([FromQuery] int rewardId, [FromQuery] decimal baseAmount)
        {
            var customerId = GetCustomerId();
            if (customerId == null)
                return Unauthorized(new { error = "UNAUTHORIZED", message = "Cần đăng nhập để simulate đổi điểm." });

            if (baseAmount <= 0)
                return BadRequest(new { error = "INVALID_AMOUNT", message = "baseAmount phải > 0." });

            try
            {
                var result = await _pointService.SimulateRedemptionAsync(customerId.Value, rewardId, baseAmount);
                return Ok(result);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("NOT_FOUND"))
                    return NotFound(new { error = "NOT_FOUND", message = ex.Message });
                return BadRequest(new { error = "BAD_REQUEST", message = ex.Message });
            }
        }

        private int? GetCustomerId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("nameid")?.Value
                ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst("sub")?.Value;
            return claim != null ? int.Parse(claim) : null;
        }
    }
}
