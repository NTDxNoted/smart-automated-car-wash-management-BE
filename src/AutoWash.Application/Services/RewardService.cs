using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Entities;

namespace AutoWash.Application.Services
{
    public class RewardService : IRewardService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<RewardService> _logger;

        public RewardService(IApplicationDbContext context, ILogger<RewardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        private static readonly HashSet<string> ValidDiscountTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "Fixed_Amount", "Percentage"
        };

        public async Task<IEnumerable<RewardResponse>> GetActiveRewardsAsync()
        {
            return await _context.RewardsCatalog
                .Where(r => r.IsActive)
                .Select(r => new RewardResponse
                {
                    RewardId = r.RewardID,
                    RewardName = r.RewardName,
                    Description = r.Description,
                    PointsRequired = r.PointsRequired,
                    DiscountAmount = r.DiscountAmount,
                    DiscountType = r.DiscountType,
                    IsActive = r.IsActive
                })
                .ToListAsync();
        }

        public async Task<RewardResponse> CreateRewardAsync(CreateRewardRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RewardName) ||
                string.IsNullOrWhiteSpace(request.Description) ||
                request.PointsRequired <= 0)
                throw new Exception("INVALID_REQUEST: Thiếu thông tin bắt buộc.");

            if (!string.IsNullOrWhiteSpace(request.DiscountType) && !ValidDiscountTypes.Contains(request.DiscountType))
                throw new Exception("INVALID_REQUEST: DiscountType phải là 'Fixed_Amount' hoặc 'Percentage'.");

            var reward = new RewardsCatalog
            {
                RewardName = request.RewardName,
                Description = request.Description,
                PointsRequired = request.PointsRequired,
                DiscountAmount = request.DiscountAmount,
                DiscountType = string.IsNullOrWhiteSpace(request.DiscountType) ? "Fixed_Amount" : request.DiscountType,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.RewardsCatalog.Add(reward);
            await _context.SaveChangesAsync();
            return MapToResponse(reward);
        }

        public async Task<RewardResponse> UpdateRewardAsync(int rewardId, UpdateRewardRequest request)
        {
            var reward = await _context.RewardsCatalog.FirstOrDefaultAsync(r => r.RewardID == rewardId);
            if (reward == null)
                throw new Exception("NOT_FOUND: Không tìm thấy phần thưởng.");

            if (request.DiscountType != null && !ValidDiscountTypes.Contains(request.DiscountType))
                throw new Exception("INVALID_REQUEST: DiscountType phải là 'Fixed_Amount' hoặc 'Percentage'.");

            if (request.RewardName != null) reward.RewardName = request.RewardName;
            if (request.Description != null) reward.Description = request.Description;
            if (request.PointsRequired.HasValue) reward.PointsRequired = request.PointsRequired.Value;
            if (request.DiscountAmount.HasValue) reward.DiscountAmount = request.DiscountAmount.Value;
            if (request.DiscountType != null) reward.DiscountType = request.DiscountType;

            await _context.SaveChangesAsync();
            return MapToResponse(reward);
        }

        public async Task<RewardResponse> ToggleRewardStatusAsync(int rewardId)
        {
            var reward = await _context.RewardsCatalog.FirstOrDefaultAsync(r => r.RewardID == rewardId);
            if (reward == null)
                throw new Exception("NOT_FOUND: Không tìm thấy phần thưởng.");

            reward.IsActive = !reward.IsActive;
            await _context.SaveChangesAsync();
            return MapToResponse(reward);
        }

        private static RewardResponse MapToResponse(RewardsCatalog r) => new RewardResponse
        {
            RewardId = r.RewardID,
            RewardName = r.RewardName,
            Description = r.Description,
            PointsRequired = r.PointsRequired,
            DiscountAmount = r.DiscountAmount,
            DiscountType = r.DiscountType,
            IsActive = r.IsActive
        };
    }
}
