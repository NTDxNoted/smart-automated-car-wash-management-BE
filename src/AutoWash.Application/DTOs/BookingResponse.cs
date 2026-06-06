using System;

namespace AutoWash.Application.DTOs
{
  public class BookingResponse
  {
    public int BookingId { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public ServiceInfoDto Service { get; set; } = new ServiceInfoDto();
    public DateTime ScheduledTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public BookingInvoiceDto Invoice { get; set; } = new BookingInvoiceDto();
    public int PointsWillEarn { get; set; }
    public DateTime CreatedAt { get; set; }
  }

  public class ServiceInfoDto
  {
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int Duration { get; set; }
  }
}
