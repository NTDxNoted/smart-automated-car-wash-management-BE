using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AutoWash.Application.Interfaces;
using AutoWash.Application.DTOs;

namespace AutoWashPro.API.Controllers
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

        // 1. POST /api/Bookings
        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
        {
            try
            {
                var customerIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                int? customerId = customerIdClaim != null ? int.Parse(customerIdClaim) : null;

                var result = await _bookingsService.CreateBookingAsync(request, customerId);
                return StatusCode(201, result);
            }
            catch (Exception ex)
            {
                string message = ex.Message;

                if (message.Contains("PENDING_QUOTA_EXCEEDED"))
                    return StatusCode(422, new { error = "PENDING_QUOTA_EXCEEDED", message });

                if (message.Contains("SLOT_NOT_AVAILABLE"))
                    return StatusCode(422, new { error = "SLOT_NOT_AVAILABLE", message });

                if (message.Contains("VEHICLE_BUFFER_VIOLATION"))
                    return StatusCode(422, new { error = "VEHICLE_BUFFER_VIOLATION", message });

                if (message.Contains("ADVANCE_NOTICE_VIOLATION"))
                    return StatusCode(422, new { error = "ADVANCE_NOTICE_VIOLATION", message });

                if (message.Contains("BOOKING_WINDOW_VIOLATION"))
                    return StatusCode(422, new { error = "BOOKING_WINDOW_VIOLATION", message });

                if (message.Contains("SUSPENDED_ACCOUNT"))
                    return StatusCode(422, new { error = "SUSPENDED_ACCOUNT", message });

                if (message.Contains("VEHICLE_REQUIRED"))
                    return BadRequest(new { error = "VEHICLE_REQUIRED", message });

                return BadRequest(new { error = "BAD_REQUEST", message });
            }
        }

        // 2. GET /api/Bookings
        [HttpGet]
        public async Task<IActionResult> GetBookings(
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? guestPhone = null)
        {
            var customerIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int? customerId = customerIdClaim != null ? int.Parse(customerIdClaim) : null;

            var result = await _bookingsService.GetCustomerBookingsAsync(
                customerId,
                guestPhone,
                status,
                page,
                pageSize);

            return Ok(result);
        }

        // 3. GET /api/Bookings/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookingById(int id, [FromQuery] string? guestPhone = null)
        {
            var customerIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int? customerId = customerIdClaim != null ? int.Parse(customerIdClaim) : null;

            var result = await _bookingsService.GetBookingByIdAsync(id, customerId, guestPhone);
            return Ok(result);
        }

        // 4. POST /api/Bookings/{id}/cancel
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

                if (message.Contains("UNAUTHORIZED"))
                    return Unauthorized(new { error = "UNAUTHORIZED", message });

                if (message.Contains("NOT_FOUND"))
                    return NotFound(new { error = "NOT_FOUND", message });

                return BadRequest(new { error = "BAD_REQUEST", message });
            }
        }

        // 5. POST /api/Bookings/{id}/complete
        [HttpPost("{id}/complete")]
        public async Task<IActionResult> CompleteBooking(int id)
        {
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(403, new
                {
                    error = "FORBIDDEN",
                    message = "Chỉ Admin hoặc Staff mới được đánh dấu hoàn thành."
                });
            }

            try
            {
                var result = await _bookingsService.CompleteBookingAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                string message = ex.Message;

                if (message.Contains("NOT_FOUND"))
                    return NotFound(new { error = "NOT_FOUND", message });

                if (message.Contains("INVALID_STATUS"))
                    return BadRequest(new { error = "INVALID_STATUS", message });

                return BadRequest(new { error = "BAD_REQUEST", message });
            }
        }

        // 6. GET /api/Bookings/available-slots
        [HttpGet("available-slots")]
        public async Task<IActionResult> GetAvailableSlots(
            [FromQuery] string? date = null,
            [FromQuery] string? licensePlate = null)
        {
            try
            {
                var customerIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                int? customerId = customerIdClaim != null ? int.Parse(customerIdClaim) : null;

                var result = await _bookingsService.GetAvailableSlotsAsync(customerId, date, licensePlate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "BAD_REQUEST", message = ex.Message });
            }
        }
    }
}