using AutoWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Threading;
using System.Threading.Tasks;

namespace AutoWash.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Booking> Bookings { get; }
        DbSet<Customer> Customers { get; }
        DbSet<LoyaltyAccount> LoyaltyAccounts { get; }
        DbSet<PointTransaction> PointTransactions { get; }
        DbSet<Service> Services { get; }
        DbSet<Vehicle> Vehicles { get; }
        DbSet<Promotion> Promotions { get; }
        DbSet<Reward> Rewards { get; }
        DbSet<CustomerPromotion> CustomerPromotions { get; }

        DatabaseFacade Database { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}