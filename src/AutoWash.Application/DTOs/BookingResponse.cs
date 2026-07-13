namespace AutoWash.Application.DTOs
{
  public class BookingResponse
  {
    public int BookingId { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public object Service { get; set; } = new { };
    public DateTime ScheduledTime { get; set; }
    public string Status { get; set; } = "Pending";
    public InvoiceSummary Invoice { get; set; } = new InvoiceSummary();
    public int PointsWillEarn { get; set; }
    public DateTime CreatedAt { get; set; }
  }
}
