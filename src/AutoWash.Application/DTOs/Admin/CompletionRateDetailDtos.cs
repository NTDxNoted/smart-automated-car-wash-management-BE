namespace AutoWash.Application.DTOs.Admin
{
  public class CompletionRateDetailResponse
  {
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string FilterType { get; set; } = string.Empty;
    public string GroupBy { get; set; } = string.Empty;
    public CompletionOverviewDto Overview { get; set; } = new();
    public CompletionRateFormulaDto Formula { get; set; } = new();
    public List<StatusBreakdownItemDto> StatusBreakdown { get; set; } = new();
    public List<TrendPointDto> Trend { get; set; } = new();
    public List<FailureReasonItemDto> FailureReasons { get; set; } = new();
    public List<ServiceCompletionItemDto> TopServices { get; set; } = new();
    public List<TimeSlotCompletionItemDto> TimeSlots { get; set; } = new();
    public List<AlertDto> Alerts { get; set; } = new();
    public PeriodComparisonDto PeriodComparison { get; set; } = new();
    public KpiDto Kpi { get; set; } = new();
    public List<string> Insights { get; set; } = new();
  }

  public class CompletionOverviewDto
  {
    public int TotalBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int CancelledBookings { get; set; }
    public int NoShowBookings { get; set; }
    public int PendingBookings { get; set; }
    public int ProcessingBookings { get; set; }
    public int FailedBookings { get; set; }
    public decimal CompletionRate { get; set; }
  }

  public class CompletionRateFormulaDto
  {
    public string Description { get; set; } = "Tỷ lệ hoàn thành = Booking hoàn thành / Tổng booking × 100%";
    public int CompletedBookings { get; set; }
    public int TotalBookings { get; set; }
    public decimal ResultPercentage { get; set; }
  }

  public class StatusBreakdownItemDto
  {
    public string Status { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
  }

  public class TrendPointDto
  {
    public string PeriodLabel { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public int TotalBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int CancelledBookings { get; set; }
    public int NoShowBookings { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal CancellationRate { get; set; }
    public decimal NoShowRate { get; set; }
  }

  public class FailureReasonItemDto
  {
    public string Status { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
  }

  public class ServiceCompletionItemDto
  {
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int TotalBookings { get; set; }
    public int CompletedBookings { get; set; }
    public decimal CompletionRate { get; set; }
  }

  public class TimeSlotCompletionItemDto
  {
    public string TimeSlot { get; set; } = string.Empty;
    public int TotalBookings { get; set; }
    public int CompletedBookings { get; set; }
    public decimal CompletionRate { get; set; }
  }

  public class UnfinishedBookingItemDto
  {
    public int BookingId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
  }

  public class UnfinishedBookingsPageDto
  {
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public List<UnfinishedBookingItemDto> Items { get; set; } = new();
  }

  public class AlertDto
  {
    public string Severity { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
  }

  public class PeriodComparisonDto
  {
    public decimal CurrentCompletionRate { get; set; }
    public decimal PreviousCompletionRate { get; set; }
    public decimal DeltaPercentagePoints { get; set; }
    public string TrendDirection { get; set; } = string.Empty;
  }

  public class KpiDto
  {
    public string Level { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
  }
}
