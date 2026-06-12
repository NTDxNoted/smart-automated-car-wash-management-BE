namespace AutoWash.Domain.Entities
{
  public class VwCustomerRfm
  {
    public int CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string CurrentTier { get; set; } = string.Empty;
    public int RecencyDays { get; set; }
    public int Frequency { get; set; }
    public decimal MonetaryTotal { get; set; }
    public int TotalPoints { get; set; }
    public decimal TotalSpending { get; set; }
    public DateTime MemberSince { get; set; }
  }
}
