namespace AutoWash.Application.DTOs
{
    public class UpdateServiceRequest
    {
        public string? ServiceName { get; set; }
        public string? ServiceCategory { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public int? Duration { get; set; }
    }
}
