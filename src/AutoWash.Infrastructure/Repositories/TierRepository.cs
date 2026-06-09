using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoWash.Domain.Entities;
using AutoWash.Infrastructure.Data;

namespace AutoWash.Infrastructure.Repositories
{
    public class TierRepository
    {
        private readonly ApplicationDbContext _context;

        public TierRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Tier>> GetAllAsync()
        {
            return await _context.Tiers.ToListAsync();
        }

        public async Task<Tier?> GetByIdAsync(int tierId)
        {
            return await _context.Tiers.FirstOrDefaultAsync(t => t.TierID == tierId);
        }
    }
}
