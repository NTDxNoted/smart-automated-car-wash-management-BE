using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;

namespace AutoWashPro.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/services")]
    [Authorize(Roles = "ADMIN")]
    public class AdminServiceController : ControllerBase
    {
        private readonly IServiceService _serviceService;

        public AdminServiceController(IServiceService serviceService)
        {
            _serviceService = serviceService;
        }

        // GET /api/admin/services
        [HttpGet]
        public async Task<IActionResult> GetServices()
        {
            var services = await _serviceService.GetAllServicesForAdminAsync();
            return Ok(services);
        }

        // POST /api/admin/services
        [HttpPost]
        public async Task<IActionResult> CreateService([FromBody] CreateServiceRequest request)
        {
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

        // DELETE /api/admin/services/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteService(int id)
        {
            try
            {
                var service = await _serviceService.DeleteServiceAsync(id);
                return Ok(service);
            }
            catch (Exception ex) when (ex.Message.StartsWith("NOT_FOUND"))
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
