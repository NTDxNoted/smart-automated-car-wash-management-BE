using AutoWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace AutoWash.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Customer> Customers { get; }
        DbSet<Vehicle> Vehicles { get; }
        DbSet<Booking> Bookings { get; }
        DbSet<LoyaltyAccount> LoyaltyAccounts { get; }
        DbSet<PointTransaction> PointTransactions { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}