using AutoWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoWash.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Booking> Bookings { get; }
        DbSet<LoyaltyAccount> LoyaltyAccounts { get; }
        DbSet<PointTransaction> PointTransactions { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}