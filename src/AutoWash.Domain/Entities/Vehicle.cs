using System;

namespace AutoWash.Domain.Entities
{
    public class Vehicle
    {
        public int VehicleID { get; set; }
        public int CustomerID { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
