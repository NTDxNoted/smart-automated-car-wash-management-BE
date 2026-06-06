using System.Threading.Tasks;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api")]
    public class ServiceController : ControllerBase
    {
        private readonly IServiceService _serviceService;
        private readonly IRewardService _rewardService;

        public ServiceController(IServiceService serviceService, IRewardService rewardService)
        {
            _serviceService = serviceService;
            _rewardService = rewardService;
        }

        // GET /api/services — public, không cần auth
        [HttpGet("services")]
        public async Task<IActionResult> GetServices()
        {
            var result = await _serviceService.GetActiveServicesAsync();
            return Ok(new { data = result }); // Bọc bằng "data" theo chuẩn response mong đợi
        }

        // GET /api/services/{id} — chi tiết dịch vụ
        [HttpGet("services/{id:int}")]
        public async Task<IActionResult> GetServiceById(int id)
        {
            var result = await _serviceService.GetServiceByIdAsync(id);
            if (result == null) return NotFound(new { message = "Service not found" });
            return Ok(result);
        }

        // GET /api/rewards — danh sách rewards active (cần auth Member)
        [Authorize(Roles = "Member")] 
        [HttpGet("rewards")]
        public async Task<IActionResult> GetRewards()
        {
            var result = await _rewardService.GetActiveRewardsAsync();
            return Ok(new { data = result });
        }
    }
}