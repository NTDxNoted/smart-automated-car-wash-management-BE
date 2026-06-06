// Application/DTOs/ServiceResponse.cs
namespace Application.DTOs
{
    public class ServiceResponse
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceCategory { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Duration { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}