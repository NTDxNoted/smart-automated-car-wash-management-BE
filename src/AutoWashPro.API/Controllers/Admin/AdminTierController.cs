using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;

namespace AutoWashPro.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/tiers")]
    [Authorize(Roles = "ADMIN")]
    public class AdminTierController : ControllerBase
    {
        private readonly ITierService _tierService;

        public AdminTierController(ITierService tierService)
        {
            _tierService = tierService;
        }

        // GET /api/admin/tiers
        [HttpGet]
        public async Task<IActionResult> GetAllTiers()
        {
            var tiers = await _tierService.GetAllTiersAsync();
            return Ok(new { data = tiers });
        }

        // PUT /api/admin/tiers/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTier(int id, [FromBody] UpdateTierRequest request)
        {
            try
            {
                var tier = await _tierService.UpdateTierAsync(id, request);
                return Ok(tier);
            }
            catch (System.Exception ex) when (ex.Message.StartsWith("NOT_FOUND"))
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
