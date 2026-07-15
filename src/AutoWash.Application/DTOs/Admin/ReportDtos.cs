namespace AutoWash.Application.DTOs.Admin
{
  public class OverviewReportResponse
  {
    public string Period { get; set; } = string.Empty;
    public int TotalBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int FailedBookings { get; set; }
    public int NoShowBookings { get; set; }
    public int CancelledBookings { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal NoShowRate { get; set; }
    public decimal AvgOrderValue { get; set; }
  }

  public class RfmReportResponse
  {
    public int CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string CurrentTier { get; set; } = string.Empty;
    public int? RecencyDays { get; set; }
    public int Frequency { get; set; }
    public decimal MonetaryTotal { get; set; }
    public int? TotalPoints { get; set; }
    public decimal TotalSpending { get; set; }
    public DateTime MemberSince { get; set; }
  }

  public class TierDistributionResponse
  {
    public string Tier { get; set; } = string.Empty;
    public int CustomerCount { get; set; }
    public decimal Percentage { get; set; }
  }

  public class LoyaltyStatsResponse
  {
    public int TotalPointsInCirculation { get; set; }
    public int PointsExpiringSoon { get; set; }
    public int ExpiredPoints { get; set; }
  }
}
