using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using AutoWash.Application.Interfaces;

namespace AutoWashPro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        // GET: /api/bookings
        [HttpGet]
        public async Task<IActionResult> GetBookings([FromQuery] int customerId, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _bookingService.GetCustomerBookingsAsync(customerId, status, page, pageSize);
            return Ok(result);
        }

        // GET: /api/bookings/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookingById(int id, [FromQuery] int customerId)
        {
            try
            {
                var result = await _bookingService.GetBookingByIdAsync(id, customerId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // POST: /api/bookings/{id}/cancel
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelBooking(int id, [FromQuery] int customerId)
        {
            try
            {
                var result = await _bookingService.CancelBookingAsync(id, customerId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Trả về lỗi 400 Bad Request đúng format mẫu
                return BadRequest(new
                {
                    error = "BAD_REQUEST",
                    message = ex.Message
                });
            }
        }
    }
}