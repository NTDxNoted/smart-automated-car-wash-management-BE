using System;

namespace AutoWash.Application.DTOs
{
    public class VehicleResponse
    {
        public int VehicleId { get; set; }
        public int CustomerId { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
