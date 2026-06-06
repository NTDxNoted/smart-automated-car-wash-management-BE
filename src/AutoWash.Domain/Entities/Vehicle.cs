namespace AutoWash.Domain.Entities
{
  public class Vehicle
  {
    public int VehicleID { get; set; }
    public int CustomerID { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
  }
}
