using AutoWash.Application.DTOs;
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
    [Route("api/admin/promotions")]
    [AuthorizeAdmin] // Guarded with Admin role validation
    public class AdminPromotionsController : ControllerBase
    {
        private readonly IPromotionService _promotionService;

        public AdminPromotionsController(IPromotionService promotionService)
        {
            _promotionService = promotionService;
        }

        // 1. GET /api/admin/promotions
        [HttpGet]
        public async Task<IActionResult> GetPromotions()
        {
            try
            {
                var result = await _promotionService.GetPromotionsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        // 2. POST /api/admin/promotions
        [HttpPost]
        public async Task<IActionResult> CreatePromotion([FromBody] CreatePromoRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (request.StartDate > request.EndDate)
                {
                    return BadRequest(new { message = "Ngày bắt đầu không được lớn hơn ngày kết thúc" });
                }

                var result = await _promotionService.CreatePromotionAsync(request);
                return StatusCode(StatusCodes.Status201Created, result);
            }
            catch (ArgumentException ex) when (ex.Message == "PROMO_CODE_EXISTS")
            {
                return BadRequest(new { message = "Mã khuyến mãi này đã tồn tại trong hệ thống" });
            }
            catch (ArgumentException ex) when (ex.Message == "PROMO_INVALID_PERCENTAGE")
            {
                return BadRequest(new { message = "Giá trị giảm giá theo % không được vượt quá 100" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        // 3. PUT /api/admin/promotions/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePromotion(int id, [FromBody] UpdatePromoRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (request.StartDate > request.EndDate)
                {
                    return BadRequest(new { message = "Ngày bắt đầu không được lớn hơn ngày kết thúc" });
                }

                var result = await _promotionService.UpdatePromotionAsync(id, request);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Không tìm thấy chương trình khuyến mãi" });
            }
            catch (ArgumentException ex) when (ex.Message == "PROMO_INVALID_PERCENTAGE")
            {
                return BadRequest(new { message = "Giá trị giảm giá theo % không được vượt quá 100" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        // 4. PATCH /api/admin/promotions/{id}/toggle
        [HttpPatch("{id}/toggle")]
        public async Task<IActionResult> TogglePromoActive(int id)
        {
            try
            {
                var result = await _promotionService.TogglePromoActiveAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Không tìm thấy chương trình khuyến mãi" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        // 5. GET /api/admin/promotions/{id}/usage
        [HttpGet("{id}/usage")]
        public async Task<IActionResult> GetPromoUsage(int id)
        {
            try
            {
                var result = await _promotionService.GetPromoUsageAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Không tìm thấy chương trình khuyến mãi" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }
    }
}
