using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/services")]
    public class AdminServiceController : ControllerBase
    {
        private readonly IServiceService _serviceService;

        public AdminServiceController(IServiceService serviceService)
        {
            _serviceService = serviceService;
        }

        // POST /api/admin/services — tạo dịch vụ mới
        [HttpPost]
        public async Task<IActionResult> CreateService([FromBody] CreateServiceRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            
            var result = await _serviceService.CreateServiceAsync(request);
            return StatusCode(201, result); // Trả về 201 Created đúng định dạng format đề bài
        }

        // PUT /api/admin/services/{id} — cập nhật giá mới
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateService(int id, [FromBody] UpdateServiceRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var updated = await _serviceService.UpdateServiceAsync(id, request);
            if (!updated) return NotFound(new { message = "Service not found" });

            return NoContent();
        }

        // PATCH /api/admin/services/{id}/status — toggle Active/Inactive
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var updated = await _serviceService.ToggleServiceStatusAsync(id);
            if (!updated) return NotFound(new { message = "Service not found" });

            return Ok(new { message = "Service status toggled successfully" });
        }
    }
}