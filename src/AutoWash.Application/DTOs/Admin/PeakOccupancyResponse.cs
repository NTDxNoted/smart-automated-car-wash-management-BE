namespace AutoWash.Application.DTOs.Admin
{
  public class PeakOccupancyResponse
  {
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public int TotalDays { get; set; }
    public int MaxParallelSlots { get; set; }
    public List<DayOfWeekStatDto> DayOfWeekStats { get; set; } = new();
    public List<HourStatDto> HourStats { get; set; } = new();
  }

  public class DayOfWeekStatDto
  {
    public string DayOfWeek { get; set; } = string.Empty;
    public int BookingCount { get; set; }
  }

  public class HourStatDto
  {
    public string TimeSlot { get; set; } = string.Empty;
    public int BookingCount { get; set; }
    public decimal OccupancyPercentage { get; set; }
  }
}
