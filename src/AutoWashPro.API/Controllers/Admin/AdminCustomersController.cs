using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AutoWash.Application.Interfaces;

namespace AutoWashPro.API.Controllers
{
    // Cấu hình route chuẩn theo yêu cầu: /api/admin/customers
    [Route("api/admin/customers")]
    [ApiController]
    // Tạm thời comment dòng [Authorize] dưới đây nếu nhóm chưa làm xong phần Login
     [Authorize(Roles = "Admin")] 
    public class AdminCustomersController : ControllerBase
    {
        private readonly IAdminCustomerService _adminCustomerService;

        public AdminCustomersController(IAdminCustomerService adminCustomerService)
        {
            _adminCustomerService = adminCustomerService;
        }

        // 1. GET /api/admin/customers
        [HttpGet]
        public async Task<IActionResult> GetCustomers([FromQuery] string? tier, [FromQuery] bool? isLocked, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await _adminCustomerService.GetCustomersAsync(tier, isLocked, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        // 2. GET /api/admin/customers/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCustomerById(int id)
        {
            try
            {
                var result = await _adminCustomerService.GetCustomerByIdAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("NOT_FOUND")) return NotFound(new { message = ex.Message });
                return BadRequest(new { message = ex.Message });
            }
        }

        // 3. PATCH /api/admin/customers/{id}/lock
        [HttpPatch("{id}/lock")]
        public async Task<IActionResult> ToggleLockCustomer(int id)
        {
            try
            {
                var result = await _adminCustomerService.ToggleLockCustomerAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("NOT_FOUND")) return NotFound(new { message = ex.Message });
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}