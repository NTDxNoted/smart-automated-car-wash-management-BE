using AutoWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoWash.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Booking> Bookings { get; set; }
        DbSet<LoyaltyAccount> LoyaltyAccounts { get; }
        DbSet<PointTransaction> PointTransactions { get; }
        DbSet<Service> Services { get; }
        DbSet<RewardsCatalog> RewardsCatalog { get; }
        DbSet<Tier> Tiers { get; }
        DbSet<Customer> Customers { get; }
        DbSet<Vehicle> Vehicles { get; }
        DbSet<Transaction> Transactions { get; }
        DbSet<Promotion> Promotions { get; set; }
        DbSet<CustomerPromotion> CustomerPromotions { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}