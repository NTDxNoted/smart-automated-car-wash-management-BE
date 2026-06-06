namespace AutoWash.Domain.Entities
{
  public class Reward
  {
    public int RewardID { get; set; }
    public int LoyaltyID { get; set; }
    public int PointsRequired { get; set; }
    public decimal DiscountAmount { get; set; }
    public bool IsActive { get; set; } = true;
  }
}
