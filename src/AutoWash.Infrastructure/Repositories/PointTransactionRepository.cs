using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoWash.Domain.Entities;
using AutoWash.Infrastructure.Data;

namespace AutoWash.Infrastructure.Repositories
{
    public class PointTransactionRepository
    {
        private readonly ApplicationDbContext _context;

        public PointTransactionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PointTransaction>> GetByLoyaltyIdAsync(int loyaltyId)
        {
            return await _context.PointTransactions
                .Where(t => t.LoyaltyID == loyaltyId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(PointTransaction transaction)
        {
            _context.PointTransactions.Add(transaction);
            await _context.SaveChangesAsync();
        }
    }
}
