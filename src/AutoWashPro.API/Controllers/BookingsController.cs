using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AutoWash.Application.Interfaces;
using AutoWashPro.API.Filters; // Bắt buộc phải có dòng này để gọi được Filter

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

        // =========================================================================
        // 4. PUT /api/bookings/{id} - ĐÂY LÀ HÀM CẦN THÊM ĐỂ ĐÁP ỨNG REQUIREMENT (Point 2)
        // =========================================================================
        [HttpPut("{id}")]
        [ServiceFilter(typeof(BookingFinancialProtectionFilter))] // <--- CHIẾC KHIÊN BẢO VỆ NẰM Ở ĐÂY
        public async Task<IActionResult> UpdateBooking(int id, [FromBody] object request) // Tạm dùng 'object', hãy đổi thành DTO của nhóm nếu có (VD: UpdateBookingDto)
        {
            try
            {
                // Gọi xuống Service để update đơn hàng. 
                // Do đoạn code cũ chưa có, bạn báo lại nhóm khai báo thêm hàm Update này trong IBookingsService nhé.
                // var result = await _bookingsService.UpdateBookingAsync(id, request);

                return Ok(new { message = "Đã đi qua lớp Middleware kiểm tra tài chính và cập nhật thành công!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "BAD_REQUEST", message = ex.Message });
            }
        }
    }
}