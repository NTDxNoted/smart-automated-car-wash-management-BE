using AutoWash.Application.DTOs.Admin;
using AutoWash.Application.Interfaces;
using AutoWashPro.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoWashPro.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/rfm")]
    [AuthorizeAdmin] // Guarded with Admin role validation
    public class AdminRfmController : ControllerBase
    {
        private readonly IPromotionService _promotionService;

        public AdminRfmController(IPromotionService promotionService)
        {
            _promotionService = promotionService;
        }

        // POST /api/admin/rfm/send-action
        [HttpPost("send-action")]
        public async Task<IActionResult> DispatchRfmAction([FromBody] RfmActionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var notification = await _promotionService.DispatchRfmActionAsync(request);
                return Ok(new
                {
                    success = true,
                    message = "Đã gửi thông báo & mã ưu đãi tới Customer thành công",
                    promoCode = notification.PromoCode
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Khách hàng không tồn tại" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }
    }
}
