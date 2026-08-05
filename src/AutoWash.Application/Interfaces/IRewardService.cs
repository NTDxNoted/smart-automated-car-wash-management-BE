using AutoWash.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoWash.Application.Interfaces
{
    public interface IRewardService
    {
        Task<IEnumerable<RewardResponse>> GetActiveRewardsAsync();
        Task<IEnumerable<RewardResponse>> GetAllRewardsAsync();
        Task<RewardResponse> CreateRewardAsync(CreateRewardRequest request);
        Task<RewardResponse> UpdateRewardAsync(int rewardId, UpdateRewardRequest request);
        Task<RewardResponse> ToggleRewardStatusAsync(int rewardId);
        Task<RewardResponse> DeleteRewardAsync(int rewardId);
    }
}
