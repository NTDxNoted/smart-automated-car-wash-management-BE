namespace Domain.Entities
{
    public class RewardsCatalog
    {
        public int RewardId { get; set; }
        public string RewardName { get; set; } = string.Empty;
        public int Points { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}