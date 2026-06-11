using AutoWash.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoWashPro.API.Controllers.Admin
{
  [ApiController]
  [Route("api/admin/reports")]
  [Authorize(Roles = "ADMIN")]
  public class AdminReportController : ControllerBase
  {
    private readonly IReportService _reportService;

    public AdminReportController(IReportService reportService)
    {
      _reportService = reportService;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
      try
      {
        var result = await _reportService.GetOverviewReportAsync();
        return Ok(result);
      }
      catch (Exception ex)
      {
        return BadRequest(new { error = "GET_OVERVIEW_FAILED", message = ex.Message });
      }
    }

    [HttpGet("rfm")]
    public async Task<IActionResult> GetRfm()
    {
      try
      {
        var result = await _reportService.GetRfmReportAsync();
        return Ok(new { data = result });
      }
      catch (Exception ex)
      {
        return BadRequest(new { error = "GET_RFM_FAILED", message = ex.Message });
      }
    }

    [HttpGet("tier-distribution")]
    public async Task<IActionResult> GetTierDistribution()
    {
      try
      {
        var result = await _reportService.GetTierDistributionAsync();
        return Ok(new { data = result });
      }
      catch (Exception ex)
      {
        return BadRequest(new { error = "GET_TIER_DISTRIBUTION_FAILED", message = ex.Message });
      }
    }

    [HttpGet("loyalty-stats")]
    public async Task<IActionResult> GetLoyaltyStats()
    {
      try
      {
        var result = await _reportService.GetLoyaltyStatsAsync();
        return Ok(result);
      }
      catch (Exception ex)
      {
        return BadRequest(new { error = "GET_LOYALTY_STATS_FAILED", message = ex.Message });
      }
    }
  }
}
