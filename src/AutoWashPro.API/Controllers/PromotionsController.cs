using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AutoWashPro.API.Controllers
{
    [ApiController]
    [Route("api/promotions")]
    public class PromotionsController : ControllerBase
    {
        private readonly IPromotionService _promotionService;

        public PromotionsController(IPromotionService promotionService)
        {
            _promotionService = promotionService;
        }

        // GET /api/promotions/validate?code=XXX
        [HttpGet("validate")]
        [ProducesResponseType(typeof(PromoValidateResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ValidatePromo([FromQuery] string code)
        {
            try
            {
                var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                            ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

                int? customerId = !string.IsNullOrEmpty(claim) && int.TryParse(claim, out int parsedId) ? parsedId : null;

                var result = await _promotionService.ValidatePromoAsync(code, customerId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                var message = ex.Message switch
                {
                    "PROMO_EXPIRED" => "Mã khuyến mãi đã hết hạn hoặc không tồn tại",
                    "PROMO_MEMBER_ONLY" => "Mã khuyến mãi chỉ dành cho thành viên đã đăng nhập",
                    "PROMO_TIER_INSUFFICIENT" => "Hạng thành viên của bạn không đủ điều kiện sử dụng mã này",
                    "PROMO_LIMIT_REACHED" => "Mã khuyến mãi đã hết lượt sử dụng",
                    "PROMO_USER_ALREADY_USED" => "Bạn đã sử dụng mã giảm giá này cho một lịch hẹn khác",
                    _ => ex.Message
                };

                return BadRequest(new
                {
                    error = ex.Message,
                    message = message
                });
            }
            catch (ArgumentException)
            {
                return BadRequest(new
                {
                    error = "PROMO_EXPIRED",
                    message = "Mã khuyến mãi không hợp lệ"
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

        // GET /api/promotions/my-notifications
        [HttpGet("my-notifications")]
        public async Task<IActionResult> GetMyNotifications()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

            int? customerId = !string.IsNullOrEmpty(claim) && int.TryParse(claim, out int parsedId) ? parsedId : null;

            var result = await _promotionService.GetMyNotificationsAsync(customerId);
            return Ok(result);
        }
    }
}
