namespace AutoWash.Application.DTOs
{
  public class PopularServiceResponse
  {
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int TotalWashes { get; set; }
    public decimal Revenue { get; set; }
    public decimal RevenueContributionPercentage { get; set; }
  }
}
