namespace AutoWash.Application.DTOs
{
    public class CreateServiceRequest
    {
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceCategory { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Duration { get; set; }
    }
}
