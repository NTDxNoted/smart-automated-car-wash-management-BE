using AutoWash.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly IBookingsService _bookingsService; // Đã đổi thành IBookingsService

    public BookingsController(IBookingsService bookingsService)
    {
        _bookingsService = bookingsService;
    }

    // 1. GET /api/bookings
    [HttpGet]
    public async Task<IActionResult> GetBookings([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? guestPhone = null)
    {
        var customerIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        int? customerId = customerIdClaim != null ? int.Parse(customerIdClaim) : null;

        var result = await _bookingsService.GetCustomerBookingsAsync(customerId, guestPhone, status, page, pageSize);
        return Ok(result);
    }

    // 2. GET /api/bookings/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetBookingById(int id, [FromQuery] string? guestPhone = null)
    {
        var customerIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        int? customerId = customerIdClaim != null ? int.Parse(customerIdClaim) : null;

        var result = await _bookingsService.GetBookingByIdAsync(id, customerId, guestPhone);
        return Ok(result);
    }

    // 3. POST /api/bookings/{id}/cancel
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelBooking(int id, [FromQuery] string? guestPhone = null)
    {
        try
        {
            // 1. Lấy ID từ Token (nếu có)
            var customerIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int? customerId = customerIdClaim != null ? int.Parse(customerIdClaim) : null;

            // 2. Gọi Service
            var result = await _bookingsService.CancelBookingAsync(id, customerId, guestPhone);
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Phân loại lỗi để trả về mã HTTP chuẩn
            string message = ex.Message;

            if (message.Contains("UNAUTHORIZED"))
            {
                return Unauthorized(new { error = "UNAUTHORIZED", message = message });
            }
            else if (message.Contains("NOT_FOUND"))
            {
                return NotFound(new { error = "NOT_FOUND", message = message });
            }
            else
            {
                // Các lỗi như INVALID_STATUS, CANCEL_TOO_LATE đều rơi vào đây
                return BadRequest(new { error = "BAD_REQUEST", message = message });
            }
        }
    }
}