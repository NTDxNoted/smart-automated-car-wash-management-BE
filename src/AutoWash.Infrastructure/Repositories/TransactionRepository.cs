using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoWash.Domain.Entities;
using AutoWash.Infrastructure.Data;

namespace AutoWash.Infrastructure.Repositories
{
    public class TransactionRepository
    {
        private readonly ApplicationDbContext _context;

        public TransactionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Transaction transaction)
        {
            await _context.Transactions.AddAsync(transaction);
        }

        public async Task<Transaction?> GetByBookingIdAsync(int bookingId)
        {
            return await _context.Transactions.FirstOrDefaultAsync(t => t.BookingID == bookingId);
        }
    }
}
