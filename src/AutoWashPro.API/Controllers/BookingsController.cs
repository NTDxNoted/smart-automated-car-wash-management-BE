using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Enums;

namespace AutoWashPro.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingsService _bookingsService;
        private readonly IGuestOtpService _guestOtpService;

        public BookingsController(IBookingsService bookingsService, IGuestOtpService guestOtpService)
        {
            _bookingsService = bookingsService;
            _guestOtpService = guestOtpService;
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

                if (message.Contains("INVALID_LICENSE_PLATE"))
                    return BadRequest(new { error = "INVALID_LICENSE_PLATE", message });

                if (message.Contains("LICENSE_PLATE_REQUIRED"))
                    return BadRequest(new { error = "LICENSE_PLATE_REQUIRED", message });

                if (message.Contains("FULLNAME_REQUIRED"))
                    return BadRequest(new { error = "FULLNAME_REQUIRED", message });

                if (message.Contains("EMAIL_REQUIRED"))
                    return BadRequest(new { error = "EMAIL_REQUIRED", message });

                if (message.Contains("EMAIL_NOT_VERIFIED"))
                    return BadRequest(new { error = "EMAIL_NOT_VERIFIED", message });

                return BadRequest(new
                {
                    error = "BAD_REQUEST",
                    message
                });
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
            try
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

        // 3. GET /api/Bookings/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookingById(int id, [FromQuery] string? guestPhone = null)
        {
            try
            {
                var customerIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                int? customerId = customerIdClaim != null ? int.Parse(customerIdClaim) : null;

                var result = await _bookingsService.GetBookingByIdAsync(id, customerId, guestPhone);
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
                return BadRequest(new
                {
                    error = "BAD_REQUEST",
                    message = ex.Message
                });
            }
        }

        // 7. POST /api/bookings/guest-email-otp/request — Guest xác thực email trước khi đặt lịch (BR mới)
        [HttpPost("guest-email-otp/request")]
        public async Task<IActionResult> RequestGuestEmailOtp([FromBody] RequestGuestEmailOtpRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(BuildValidationError());

            try
            {
                await _guestOtpService.GenerateAndSendAsync(request.Email, OtpPurpose.GuestBookingVerify);
                return Ok(new { message = "Mã OTP đã được gửi đến email của bạn." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(MapGuestOtpError(ex.Message));
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "INTERNAL_SERVER_ERROR", message = "Đã có lỗi xảy ra, vui lòng thử lại sau." });
            }
        }

        // 8. POST /api/bookings/guest-email-otp/verify
        [HttpPost("guest-email-otp/verify")]
        public async Task<IActionResult> VerifyGuestEmailOtp([FromBody] VerifyGuestEmailOtpRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(BuildValidationError());

            try
            {
                await _guestOtpService.VerifyAsync(request.Email, OtpPurpose.GuestBookingVerify, request.Code);
                return Ok(new { message = "Email đã được xác thực thành công." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(MapGuestOtpError(ex.Message));
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "INTERNAL_SERVER_ERROR", message = "Đã có lỗi xảy ra, vui lòng thử lại sau." });
            }
        }

        private object BuildValidationError()
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors);
            return new { error = "VALIDATION_FAILED", message = string.Join("; ", errors.Select(e => e.ErrorMessage)) };
        }

        private static object MapGuestOtpError(string code)
        {
            var message = code switch
            {
                "OTP_NOT_FOUND" => "Không tìm thấy mã OTP, vui lòng yêu cầu gửi lại",
                "OTP_EXPIRED" => "Mã OTP đã hết hạn, vui lòng yêu cầu gửi lại",
                "OTP_LOCKED" => "Bạn đã nhập sai quá số lần cho phép, vui lòng yêu cầu gửi lại mã",
                "OTP_INVALID" => "Mã OTP không đúng",
                "OTP_COOLDOWN" => "Vui lòng đợi một chút trước khi yêu cầu gửi lại mã",
                _ => "Đã có lỗi xảy ra, vui lòng thử lại sau."
            };

            return new { error = code, message };
        }
    }
}