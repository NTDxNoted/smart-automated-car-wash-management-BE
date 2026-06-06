using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AutoWash.Application.Interfaces;

namespace AutoWashPro.API.Controllers
{
    [ApiController]
    [Route("api/services")]
    public class ServiceController : ControllerBase
    {
        private readonly IServiceService _serviceService;

        public ServiceController(IServiceService serviceService)
        {
            _serviceService = serviceService;
        }

        // GET /api/services — public, không cần auth
        [HttpGet]
        public async Task<IActionResult> GetActiveServices()
        {
            var services = await _serviceService.GetActiveServicesAsync();
            return Ok(new { data = services });
        }

        // GET /api/services/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetServiceById(int id)
        {
            try
            {
                var service = await _serviceService.GetServiceByIdAsync(id);
                return Ok(service);
            }
            catch (Exception ex) when (ex.Message.StartsWith("NOT_FOUND"))
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
