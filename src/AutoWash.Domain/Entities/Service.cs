namespace AutoWash.Domain.Entities
{
    public class Service
    {
        public int ServiceID { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceCategory { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Duration { get; set; }
        public string Status { get; set; } = "Active";
    }
}