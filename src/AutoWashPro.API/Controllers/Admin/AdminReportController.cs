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
    public async Task<IActionResult> GetOverview([FromQuery] string? filterType, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
      try
      {
        if (startDate.HasValue && endDate.HasValue)
        {
          if (startDate.Value > endDate.Value)
            return BadRequest(new { error = "INVALID_DATE_RANGE", message = "startDate must be before or equal to endDate." });

          if ((endDate.Value.Date - startDate.Value.Date).Days > 365)
            return BadRequest(new { error = "DATE_RANGE_TOO_WIDE", message = "Date range cannot exceed 366 days." });
        }

        var result = await _reportService.GetOverviewReportAsync(filterType, startDate, endDate);
        return Ok(result);
      }
      catch (Exception ex)
      {
        return BadRequest(new { error = "GET_OVERVIEW_FAILED", message = ex.Message });
      }
    }

    [HttpGet("popular-services")]
    public async Task<IActionResult> GetPopularServices([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
      try
      {
        if (startDate.HasValue && endDate.HasValue)
        {
          if (startDate.Value > endDate.Value)
            return BadRequest(new { error = "INVALID_DATE_RANGE", message = "startDate must be before or equal to endDate." });

          if ((endDate.Value.Date - startDate.Value.Date).Days > 365)
            return BadRequest(new { error = "DATE_RANGE_TOO_WIDE", message = "Date range cannot exceed 366 days." });
        }

        var result = await _reportService.GetPopularServicesReportAsync(startDate, endDate);
        return Ok(result);
      }
      catch (Exception ex)
      {
        return BadRequest(new { error = "GET_POPULAR_SERVICES_FAILED", message = ex.Message });
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

    [HttpGet("peak-occupancy")]
    public async Task<IActionResult> GetPeakOccupancy([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
      try
      {
        if (startDate > endDate)
          return BadRequest(new { error = "INVALID_DATE_RANGE", message = "startDate must be before or equal to endDate." });

        if ((endDate.Date - startDate.Date).Days > 365)
          return BadRequest(new { error = "DATE_RANGE_TOO_WIDE", message = "Date range cannot exceed 366 days." });

        var result = await _reportService.GetPeakOccupancyReportAsync(startDate, endDate);
        return Ok(result);
      }
      catch (Exception ex)
      {
        return BadRequest(new { error = "GET_PEAK_OCCUPANCY_FAILED", message = ex.Message });
      }
    }

    [HttpGet("promotions-roi")]
    public async Task<IActionResult> GetPromotionsRoi([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
      try
      {
        if (startDate > endDate)
          return BadRequest(new { error = "INVALID_DATE_RANGE", message = "startDate must be before or equal to endDate." });

        if ((endDate.Date - startDate.Date).Days > 365)
          return BadRequest(new { error = "DATE_RANGE_TOO_WIDE", message = "Date range cannot exceed 366 days." });

        var result = await _reportService.GetPromotionRoiReportAsync(startDate, endDate);
        return Ok(result);
      }
      catch (Exception ex)
      {
        return BadRequest(new { error = "GET_PROMOTIONS_ROI_FAILED", message = ex.Message });
      }
    }

    [HttpGet("revenue-detail")]
    public async Task<IActionResult> GetRevenueDetail([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] string? paymentMethod)
    {
      try
      {
        if (startDate > endDate)
          return BadRequest(new { error = "INVALID_DATE_RANGE", message = "startDate must be before or equal to endDate." });

        if ((endDate.Date - startDate.Date).Days > 365)
          return BadRequest(new { error = "DATE_RANGE_TOO_WIDE", message = "Date range cannot exceed 366 days." });

        var result = await _reportService.GetRevenueDetailReportAsync(startDate, endDate, paymentMethod);
        return Ok(result);
      }
      catch (ArgumentException ex)
      {
        return BadRequest(new { error = "INVALID_PAYMENT_METHOD", message = ex.Message });
      }
      catch (Exception ex)
      {
        return BadRequest(new { error = "GET_REVENUE_DETAIL_FAILED", message = ex.Message });
      }
    }

    [HttpGet("completion-rate-detail")]
    public async Task<IActionResult> GetCompletionRateDetail([FromQuery] string? filterType, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] int? serviceId, [FromQuery] string? groupBy)
    {
      try
      {
        if (!IsValidDateRange(startDate, endDate, out var rangeError))
          return BadRequest(rangeError);

        var result = await _reportService.GetCompletionRateDetailAsync(filterType, startDate, endDate, serviceId, groupBy);
        return Ok(result);
      }
      catch (Exception ex)
      {
        return BadRequest(new { error = "GET_COMPLETION_RATE_DETAIL_FAILED", message = ex.Message });
      }
    }

    [HttpGet("completion-rate-detail/unfinished-bookings")]
    public async Task<IActionResult> GetUnfinishedBookings([FromQuery] string? filterType, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] int? serviceId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
      try
      {
        if (!IsValidDateRange(startDate, endDate, out var rangeError))
          return BadRequest(rangeError);

        var result = await _reportService.GetUnfinishedBookingsAsync(filterType, startDate, endDate, serviceId, page, pageSize);
        return Ok(result);
      }
      catch (Exception ex)
      {
        return BadRequest(new { error = "GET_UNFINISHED_BOOKINGS_FAILED", message = ex.Message });
      }
    }

    [HttpGet("completion-rate-detail/export")]
    public async Task<IActionResult> ExportUnfinishedBookings([FromQuery] string? filterType, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] int? serviceId)
    {
      try
      {
        if (!IsValidDateRange(startDate, endDate, out var rangeError))
          return BadRequest(rangeError);

        var bytes = await _reportService.ExportUnfinishedBookingsAsync(filterType, startDate, endDate, serviceId);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "unfinished-bookings.xlsx");
      }
      catch (Exception ex)
      {
        return BadRequest(new { error = "EXPORT_UNFINISHED_BOOKINGS_FAILED", message = ex.Message });
      }
    }

    private static bool IsValidDateRange(DateTime? startDate, DateTime? endDate, out object? error)
    {
      error = null;
      if (!startDate.HasValue || !endDate.HasValue)
        return true;

      if (startDate.Value > endDate.Value)
      {
        error = new { error = "INVALID_DATE_RANGE", message = "startDate must be before or equal to endDate." };
        return false;
      }

      if ((endDate.Value.Date - startDate.Value.Date).Days > 365)
      {
        error = new { error = "DATE_RANGE_TOO_WIDE", message = "Date range cannot exceed 366 days." };
        return false;
      }

      return true;
    }
  }
}
