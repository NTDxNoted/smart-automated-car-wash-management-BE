namespace AutoWash.Application.DTOs.Admin
{
    public class PopularServiceResponse
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal Percentage { get; set; }
    }
}
