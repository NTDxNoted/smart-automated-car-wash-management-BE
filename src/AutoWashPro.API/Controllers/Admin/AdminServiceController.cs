using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;

namespace AutoWashPro.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/services")]
    public class AdminServiceController : ControllerBase
    {
        private readonly IServiceService _serviceService;

        public AdminServiceController(IServiceService serviceService)
        {
            _serviceService = serviceService;
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

        // POST /api/admin/services
        [HttpPost]
        public async Task<IActionResult> CreateService([FromBody] CreateServiceRequest request)
        {
            var authError = CheckAdminAccess();
            if (authError != null) return authError;

            try
            {
                var service = await _serviceService.CreateServiceAsync(request);
                return Created($"/api/services/{service.ServiceId}", service);
            }
            catch (Exception ex) when (ex.Message.StartsWith("INVALID_REQUEST"))
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT /api/admin/services/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateService(int id, [FromBody] UpdateServiceRequest request)
        {
            var authError = CheckAdminAccess();
            if (authError != null) return authError;

            try
            {
                var service = await _serviceService.UpdateServiceAsync(id, request);
                return Ok(service);
            }
            catch (Exception ex) when (ex.Message.StartsWith("NOT_FOUND"))
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // PATCH /api/admin/services/{id}/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ToggleServiceStatus(int id)
        {
            var authError = CheckAdminAccess();
            if (authError != null) return authError;

            try
            {
                var service = await _serviceService.ToggleServiceStatusAsync(id);
                return Ok(service);
            }
            catch (Exception ex) when (ex.Message.StartsWith("NOT_FOUND"))
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
