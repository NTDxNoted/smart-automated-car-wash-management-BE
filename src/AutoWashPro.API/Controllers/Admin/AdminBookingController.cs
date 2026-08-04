using System;
using System.Threading.Tasks;
using AutoWash.Application.DTOs.Admin;
using AutoWash.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoWashPro.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/bookings")]
    [Authorize(Roles = "ADMIN")]
    public class AdminBookingController : ControllerBase
    {
        private readonly IAdminBookingService _adminBookingService;

        public AdminBookingController(IAdminBookingService adminBookingService)
        {
            _adminBookingService = adminBookingService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllBookings([FromQuery] string? status, [FromQuery] string? date, [FromQuery] string? phone, [FromQuery] string? plate)
        {
            try
            {
                var bookings = await _adminBookingService.GetAllBookingsAsync(status, date, phone, plate);
                return Ok(bookings);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "GET_ALL_BOOKINGS_FAILED", message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookingById(int id)
        {
            try
            {
                var booking = await _adminBookingService.GetBookingByIdAsync(id);
                return Ok(booking);
            }
            catch (Exception ex)
            {
                if (ex.Message == "BOOKING_NOT_FOUND")
                    return NotFound(new { error = "BOOKING_NOT_FOUND", message = "Không tìm thấy lịch hẹn" });
                return BadRequest(new { error = "GET_BOOKING_FAILED", message = ex.Message });
            }
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateBookingStatus(int id, [FromBody] UpdateBookingStatusRequest request)
        {
            try
            {
                var result = await _adminBookingService.UpdateBookingStatusAsync(id, request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = "INVALID_STATUS_TRANSITION", message = ex.Message });
            }
            catch (Exception ex)
            {
                if (ex.Message == "BOOKING_NOT_FOUND")
                    return NotFound(new { error = "BOOKING_NOT_FOUND", message = "Không tìm thấy lịch hẹn" });
                return BadRequest(new { error = "UPDATE_STATUS_FAILED", message = ex.Message });
            }
        }

        [HttpPatch("{id}/license-plate")]
        public async Task<IActionResult> UpdateLicensePlate(int id, [FromBody] UpdateLicensePlateRequest request)
        {
            try
            {
                var result = await _adminBookingService.UpdateLicensePlateAsync(id, request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = "INVALID_OPERATION", message = ex.Message });
            }
            catch (Exception ex)
            {
                if (ex.Message == "BOOKING_NOT_FOUND")
                    return NotFound(new { error = "BOOKING_NOT_FOUND", message = "Không tìm thấy lịch hẹn" });
                return BadRequest(new { error = "UPDATE_PLATE_FAILED", message = ex.Message });
            }
        }

        [HttpPost("walk-in")]
        public async Task<IActionResult> CreateWalkInBooking([FromBody] CreateWalkInBookingRequest request)
        {
            try
            {
                var result = await _adminBookingService.CreateWalkInBookingAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                if (ex.Message.StartsWith("SERVICE_NOT_FOUND"))
                    return NotFound(new { error = "SERVICE_NOT_FOUND", message = "Không tìm thấy dịch vụ" });
                return BadRequest(new { error = "CREATE_WALK_IN_BOOKING_FAILED", message = ex.Message });
            }
        }

        [HttpPost("{id}/checkin")]
        public async Task<IActionResult> CheckIn(int id)
        {
            try
            {
                var result = await _adminBookingService.CheckInAsync(id);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = "INVALID_OPERATION", message = ex.Message });
            }
            catch (Exception ex)
            {
                if (ex.Message == "BOOKING_NOT_FOUND")
                    return NotFound(new { error = "BOOKING_NOT_FOUND", message = "Không tìm thấy lịch hẹn" });
                return BadRequest(new { error = "CHECKIN_FAILED", message = ex.Message });
            }
        }

        [HttpPost("{id}/emergency-stop")]
        public async Task<IActionResult> EmergencyStop(int id, [FromBody] EmergencyStopRequest request)
        {
            try
            {
                var result = await _adminBookingService.EmergencyStopAsync(id, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                if (ex.Message == "BOOKING_NOT_FOUND")
                    return NotFound(new { error = "BOOKING_NOT_FOUND", message = "Không tìm thấy lịch hẹn" });
                return BadRequest(new { error = "EMERGENCY_STOP_FAILED", message = ex.Message });
            }
        }
    }
}
