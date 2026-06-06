using System;

namespace AutoWash.Application.DTOs
{
  public class CreateGuestBookingRequest : CreateBookingRequest
  {
    public new string Phone { get; set; } = string.Empty;
    public new string LicensePlate { get; set; } = string.Empty;
  }
}
