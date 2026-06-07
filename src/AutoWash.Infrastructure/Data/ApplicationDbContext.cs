using Microsoft.EntityFrameworkCore;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Entities;

namespace AutoWash.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
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

            builder.Entity<Customer>(entity =>
            {
                entity.ToTable("customer");
                entity.HasKey(e => e.CustomerID);

                entity.Property(e => e.CustomerID).HasColumnName("customerid");
                entity.Property(e => e.FullName).HasColumnName("fullname");
                entity.Property(e => e.Phone).HasColumnName("phone");
                entity.Property(e => e.Password).HasColumnName("password");
                entity.Property(e => e.TotalSpending).HasColumnName("totalspending");
                entity.Property(e => e.LastVisit).HasColumnName("lastvisit");
                entity.Property(e => e.IsLocked).HasColumnName("islocked");
                entity.Property(e => e.SuspendedUntil).HasColumnName("suspendeduntil");
                entity.Property(e => e.CreatedAt).HasColumnName("createdat");

                entity.HasIndex(e => e.Phone).IsUnique();
            });

            builder.Entity<LoyaltyAccount>(entity =>
            {
                entity.ToTable("loyaltyaccount");
                entity.HasKey(e => e.LoyaltyID);

                entity.Property(e => e.LoyaltyID).HasColumnName("loyaltyid");
                entity.Property(e => e.CustomerID).HasColumnName("customerid");
                entity.Property(e => e.TotalPoints).HasColumnName("totalpoints");
                entity.Property(e => e.LastUpdated).HasColumnName("lastupdated");
            });

            builder.Entity<Vehicle>(entity =>
            {
                entity.ToTable("vehicle");
                entity.HasKey(e => e.VehicleID);

                entity.Property(e => e.VehicleID).HasColumnName("vehicleid");
                entity.Property(e => e.CustomerID).HasColumnName("customerid");
                entity.Property(e => e.LicensePlate).HasColumnName("licenseplate");

                entity.HasIndex(e => new { e.CustomerID, e.LicensePlate }).IsUnique();
            });

            builder.Entity<Booking>(entity =>
            {
                entity.ToTable("booking");
                entity.HasKey(e => e.BookingID);

                entity.Property(e => e.BookingID).HasColumnName("bookingid");
                entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>();
            });

            builder.Entity<PointTransaction>(entity =>
            {
                entity.ToTable("pointtransaction");
                entity.HasKey(e => e.PointTxnID);

                entity.Property(e => e.PointTxnID).HasColumnName("pointtxnid");
                entity.Property(e => e.Type).HasColumnName("type").HasConversion<string>();
            });

            builder.Entity<Service>(entity =>
            {
                entity.ToTable("service");
                entity.HasKey(e => e.ServiceID);

                entity.Property(e => e.ServiceID).HasColumnName("serviceid");
            });

            builder.Entity<RewardsCatalog>(entity =>
            {
                entity.ToTable("rewardscatalog");
                entity.HasKey(e => e.RewardID);

                entity.Property(e => e.RewardID).HasColumnName("rewardid");
            });
        }
    }
}