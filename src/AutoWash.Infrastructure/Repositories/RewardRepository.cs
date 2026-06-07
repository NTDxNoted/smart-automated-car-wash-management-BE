using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Entities;

namespace AutoWash.Infrastructure.Repositories
{
    public class RewardRepository
    {
        private readonly IApplicationDbContext _context;

        public RewardRepository(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<RewardsCatalog>> GetActiveRewardsAsync()
        {
            return await _context.RewardsCatalog
                .Where(r => r.IsActive)
                .ToListAsync();
        }

        public async Task<RewardsCatalog?> GetByIdAsync(int rewardId)
        {
            return await _context.RewardsCatalog.FirstOrDefaultAsync(r => r.RewardID == rewardId);
        }
    }
}
