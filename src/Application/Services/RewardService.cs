// Application/Services/RewardService.cs
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services
{
    public class RewardService : IRewardService
    {
        private readonly IRewardRepository _repository;

        public RewardService(IRewardRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<RewardResponse>> GetActiveRewardsAsync()
        {
            var rewards = await _repository.GetAllAsync();
            return rewards.Where(r => r.IsActive).Select(MapToResponse);
        }

        public async Task<RewardResponse> CreateRewardAsync(CreateRewardRequest request)
        {
            var reward = new RewardsCatalog
            {
                RewardName = request.RewardName,
                Points = request.Points,
                Description = request.Description,
                IsActive = true
            };
            var created = await _repository.AddAsync(reward);
            return MapToResponse(created);
        }

        public async Task<bool> UpdateRewardAsync(int id, CreateRewardRequest request)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return false;

            existing.RewardName = request.RewardName;
            existing.Points = request.Points;
            existing.Description = request.Description;

            await _repository.UpdateAsync(existing);
            return true;
        }

        public async Task<bool> ToggleRewardStatusAsync(int id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return false;

            existing.IsActive = !existing.IsActive;
            await _repository.UpdateAsync(existing);
            return true;
        }

        private RewardResponse MapToResponse(RewardsCatalog reward) => new()
        {
            RewardId = reward.RewardId,
            RewardName = reward.RewardName,
            Points = reward.Points,
            Description = reward.Description,
            IsActive = reward.IsActive
        };
    }
}