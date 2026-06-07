using Microsoft.EntityFrameworkCore;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Entities;

namespace AutoWash.Infrastructure.Data
{
    // Kế thừa IApplicationDbContext để đảm bảo tuân thủ hợp đồng từ tầng Application
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<LoyaltyAccount> LoyaltyAccounts { get; set; }
        public DbSet<PointTransaction> PointTransactions { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<RewardsCatalog> RewardsCatalog { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Customer>().ToTable("customer").HasKey(c => c.CustomerID);
            builder.Entity<Vehicle>().ToTable("vehicle").HasKey(v => v.VehicleID);
            builder.Entity<Booking>().ToTable("booking").HasKey(b => b.BookingID);
            builder.Entity<LoyaltyAccount>().ToTable("loyaltyaccount").HasKey(l => l.LoyaltyID);
            builder.Entity<PointTransaction>().ToTable("pointtransaction").HasKey(p => p.PointTxnID);
            builder.Entity<Service>().ToTable("service").HasKey(s => s.ServiceID);
            builder.Entity<RewardsCatalog>().ToTable("rewardscatalog").HasKey(r => r.RewardID);

            // BR-09: UNIQUE (CustomerID, LicensePlate)
            builder.Entity<Vehicle>()
                .HasIndex(v => new { v.CustomerID, v.LicensePlate })
                .IsUnique();

            builder.Entity<Booking>().Property(b => b.Status).HasConversion<string>();
            builder.Entity<PointTransaction>().Property(p => p.Type).HasConversion<string>();
        }
    }
}