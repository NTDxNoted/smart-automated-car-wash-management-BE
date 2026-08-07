using System;

namespace AutoWash.Application.DTOs
{
  public class CreateBookingRequest
  {
    public string? Phone { get; set; }
    // Guest only — bắt buộc và phải đã xác thực OTP (xem BookingService.CreateBookingAsync).
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? LicensePlate { get; set; }

    public int? VehicleId { get; set; }

    public int ServiceId { get; set; }

    public DateTime ScheduledTime { get; set; }

    public string? PromoCode { get; set; }

    public int? RewardId { get; set; }
  }
}
