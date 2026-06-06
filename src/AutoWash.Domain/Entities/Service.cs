namespace AutoWash.Domain.Entities
{
  public class Service
  {
    public int ServiceID { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int Duration { get; set; }
    public decimal BaseAmount { get; set; }
    public bool IsActive { get; set; } = true;
  }
}
