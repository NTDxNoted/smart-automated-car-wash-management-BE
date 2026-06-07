using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;

namespace AutoWashPro.API.Controllers
{
    [ApiController]
    [Route("api/vehicles")]
    public class VehicleController : ControllerBase
    {
        private readonly IVehicleService _vehicleService;
        private readonly ICustomerService _customerService;
        private readonly IOtpService _otpService;

        public VehicleController(IVehicleService vehicleService, ICustomerService customerService, IOtpService otpService)
        {
            _vehicleService = vehicleService;
            _customerService = customerService;
            _otpService = otpService;
        }

        // BR-07: Chặn Guest
        private IActionResult? CheckMemberAccess(out int customerId)
        {
            customerId = 0;
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (claim == null)
                return Unauthorized(new { message = "UNAUTHORIZED: Cần đăng nhập." });
            customerId = int.Parse(claim);
            return null;
        }

        // GET /api/vehicles
        [HttpGet]
        public async Task<IActionResult> GetVehicles()
        {
            var authError = CheckMemberAccess(out int customerId);
            if (authError != null) return authError;

            var vehicles = await _vehicleService.GetVehiclesAsync(customerId);
            return Ok(new { data = vehicles });
        }

        // POST /api/vehicles/request-otp — BR-10: sinh OTP, log ra console
        [HttpPost("request-otp")]
        public async Task<IActionResult> RequestOtp()
        {
            var authError = CheckMemberAccess(out int customerId);
            if (authError != null) return authError;

            try
            {
                var profile = await _customerService.GetProfileAsync(customerId);
                _otpService.GenerateAndStore(profile.Phone);
                return Ok(new { message = "OTP đã được gửi về số điện thoại của bạn." });
            }
            catch (Exception ex) when (ex.Message.StartsWith("NOT_FOUND"))
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // POST /api/vehicles
        [HttpPost]
        public async Task<IActionResult> AddVehicle([FromBody] AddVehicleRequest request)
        {
            var authError = CheckMemberAccess(out int customerId);
            if (authError != null) return authError;

            try
            {
                var vehicle = await _vehicleService.AddVehicleAsync(customerId, request);
                return Created($"/api/vehicles/{vehicle.VehicleId}", vehicle);
            }
            catch (Exception ex) when (ex.Message.StartsWith("PLATE_ALREADY_SAVED"))
            {
                return Conflict(new { error = "PLATE_ALREADY_SAVED", message = "Biển số này đã có trong danh sách xe của bạn." });
            }
            catch (Exception ex) when (ex.Message.StartsWith("INVALID_OTP"))
            {
                return BadRequest(new { error = "INVALID_OTP", message = ex.Message });
            }
            catch (Exception ex) when (ex.Message.StartsWith("NOT_FOUND"))
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // PUT /api/vehicles/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVehicle(int id, [FromBody] UpdateVehicleRequest request)
        {
            var authError = CheckMemberAccess(out int customerId);
            if (authError != null) return authError;

            try
            {
                var vehicle = await _vehicleService.UpdateVehicleAsync(customerId, id, request);
                return Ok(vehicle);
            }
            catch (Exception ex) when (ex.Message.StartsWith("PLATE_ALREADY_SAVED"))
            {
                return Conflict(new { error = "PLATE_ALREADY_SAVED", message = "Biển số này đã có trong danh sách xe của bạn." });
            }
            catch (Exception ex) when (ex.Message.StartsWith("INVALID_OTP"))
            {
                return BadRequest(new { error = "INVALID_OTP", message = ex.Message });
            }
            catch (Exception ex) when (ex.Message.StartsWith("NOT_FOUND"))
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // DELETE /api/vehicles/{id} — xóa mềm (BR-11: chỉ xe của chính customer)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVehicle(int id)
        {
            var authError = CheckMemberAccess(out int customerId);
            if (authError != null) return authError;

            try
            {
                await _vehicleService.DeleteVehicleAsync(customerId, id);
                return NoContent();
            }
            catch (Exception ex) when (ex.Message.StartsWith("NOT_FOUND"))
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
