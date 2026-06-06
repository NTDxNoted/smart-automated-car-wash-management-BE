// IRewardService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;

namespace Application.Interfaces
{
    public interface IRewardService
    {
        Task<IEnumerable<RewardResponse>> GetActiveRewardsAsync();
        Task<RewardResponse> CreateRewardAsync(CreateRewardRequest request);
        Task<bool> UpdateRewardAsync(int id, CreateRewardRequest request);
        Task<bool> ToggleRewardStatusAsync(int id);
    }
}