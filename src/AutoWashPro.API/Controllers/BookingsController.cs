using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;


namespace AutoWashPro.API.Controllers // Nhớ giữ nguyên namespace hiện tại của nhóm bạn
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingsService _bookingsService;

        public BookingsController(IBookingsService bookingsService)
        {
            _bookingsService = bookingsService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
        {
            try
            {
                var customerIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                int? customerId = customerIdClaim != null ? int.Parse(customerIdClaim) : null;

                var result = await _bookingsService.CreateBookingAsync(request, customerId);
                return CreatedAtAction(nameof(GetBookingById), new { id = result.BookingId }, result);
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                if (message.Contains("PENDING_QUOTA_EXCEEDED") || message.Contains("SLOT_NOT_AVAILABLE") ||
                    message.Contains("VEHICLE_BUFFER_VIOLATION") || message.Contains("ADVANCE_NOTICE_TOO_SHORT") ||
                    message.Contains("ACCOUNT_SUSPENDED"))
                    return UnprocessableEntity(new { error = message.Split(':')[0], message = message.Split(':')[1].Trim() });

                if (message.Contains("NOT_FOUND")) return NotFound(new { error = "NOT_FOUND", message = message });
                return BadRequest(new { error = "BAD_REQUEST", message = message });
            }
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
                var customerIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                int? customerId = customerIdClaim != null ? int.Parse(customerIdClaim) : null;

                var result = await _bookingsService.CancelBookingAsync(id, customerId, guestPhone);
                return Ok(result);
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                if (message.Contains("UNAUTHORIZED")) return Unauthorized(new { error = "UNAUTHORIZED", message = message });
                if (message.Contains("NOT_FOUND")) return NotFound(new { error = "NOT_FOUND", message = message });
                return BadRequest(new { error = "BAD_REQUEST", message = message });
            }
        }


        // 4. POST /api/bookings/{id}/complete  (Admin/Staff only — BR-21)
        [HttpPost("{id}/complete")]
        public async Task<IActionResult> CompleteBooking(int id)
        {
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (role != "Admin" && role != "Staff")
                return StatusCode(403, new { error = "FORBIDDEN", message = "Chỉ Admin hoặc Staff mới được đánh dấu hoàn thành." });

            try
            {
                var result = await _bookingsService.CompleteBookingAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                if (message.Contains("NOT_FOUND")) return NotFound(new { error = "NOT_FOUND", message = message });
                if (message.Contains("INVALID_STATUS")) return BadRequest(new { error = "INVALID_STATUS", message = message });
                return BadRequest(new { error = "BAD_REQUEST", message = message });
            }
        }
    }
}