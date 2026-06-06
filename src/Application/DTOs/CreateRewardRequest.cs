// Application/DTOs/CreateRewardRequest.cs
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class CreateRewardRequest
    {
        [Required(ErrorMessage = "RewardName is required.")]
        public string RewardName { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Points must be at least 1.")]
        public int Points { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; } = string.Empty;
    }
}