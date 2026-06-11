namespace AutoWash.Application.DTOs
{
  public class CreateBookingRequest
  {
    public string? Phone { get; set; }
    public string? LicensePlate { get; set; }
    public int? VehicleId { get; set; }
    public int ServiceId { get; set; }
    public DateTime ScheduledTime { get; set; }
    public int? RewardId { get; set; }
    public string? PromoCode { get; set; }
  }
}
