using System;
using System.Threading.Tasks;
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

        public async Task AcquireBookingDateLockAsync(DateTime date)
        {
            if (Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) != true)
                return;

            var lockKey = "booking-slot-" + date.ToString("yyyy-MM-dd");
            await Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({lockKey}))");
        }

        public DbSet<Tier> Tiers { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<LoyaltyAccount> LoyaltyAccounts { get; set; }
        public DbSet<PointTransaction> PointTransactions { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<RewardsCatalog> RewardsCatalog { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<CustomerPromotion> CustomerPromotions { get; set; }
        public DbSet<VwCustomerRfm> VwCustomerRfm { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Tier>(entity =>
            {
                entity.ToTable("tier");
                entity.HasKey(e => e.TierID);

                entity.Property(e => e.TierID).HasColumnName("tierid");
                entity.Property(e => e.TierName).HasColumnName("tiername");
                entity.Property(e => e.MinSpending).HasColumnName("minspending");
                entity.Property(e => e.BookingWindowDays).HasColumnName("bookingwindowdays");
                entity.Property(e => e.DiscountRate).HasColumnName("discountrate");
                entity.Property(e => e.PriorityScore).HasColumnName("priorityscore");
            });

            builder.Entity<Customer>(entity =>
            {
                entity.ToTable("customer", t =>
                    t.HasCheckConstraint("CK_Customer_Role", "role IN ('MEMBER', 'ADMIN')"));

                entity.HasKey(e => e.CustomerID);

                entity.Property(e => e.CustomerID).HasColumnName("customerid");
                entity.Property(e => e.FullName).HasColumnName("fullname");
                entity.Property(e => e.Phone).HasColumnName("phone");
                entity.Property(e => e.Password).HasColumnName("password");
                entity.Property(e => e.Role).HasColumnName("role");
                entity.Property(e => e.TierID).HasColumnName("tierid");
                entity.Property(e => e.TotalSpending).HasColumnName("totalspending");
                entity.Property(e => e.LastVisit).HasColumnName("lastvisit");
                entity.Property(e => e.IsLocked).HasColumnName("islocked");
                entity.Property(e => e.SuspendedUntil).HasColumnName("suspendeduntil");
                entity.Property(e => e.ActiveSessionId).HasColumnName("activesessionid");
                entity.Property(e => e.CreatedAt).HasColumnName("createdat");

                entity.HasIndex(e => e.Phone).IsUnique();
            });

            builder.Entity<VwCustomerRfm>(entity =>
            {
                entity.ToView("vw_customer_rfm");
                entity.HasNoKey();

                entity.Property(e => e.CustomerId).HasColumnName("customerid");
                entity.Property(e => e.FullName).HasColumnName("fullname");
                entity.Property(e => e.Phone).HasColumnName("phone");
                entity.Property(e => e.CurrentTier).HasColumnName("currenttier");
                entity.Property(e => e.RecencyDays).HasColumnName("recency_days");
                entity.Property(e => e.Frequency).HasColumnName("frequency");
                entity.Property(e => e.MonetaryTotal).HasColumnName("monetary_total");
                entity.Property(e => e.TotalPoints).HasColumnName("totalpoints");
                entity.Property(e => e.TotalSpending).HasColumnName("totalspending");
                entity.Property(e => e.MemberSince).HasColumnName("membersince");
            });

            builder.Entity<LoyaltyAccount>(entity =>
            {
                entity.ToTable("loyaltyaccount");
                entity.HasKey(e => e.LoyaltyID);
                entity.HasMany(e => e.PointTransactions)
                      .WithOne()
                      .HasForeignKey(pt => pt.LoyaltyID)
                      .OnDelete(DeleteBehavior.Cascade);

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
                entity.Property(e => e.CustomerID).HasColumnName("customerid");
                entity.Property(e => e.Phone).HasColumnName("phone");
                entity.Property(e => e.VehicleID).HasColumnName("vehicleid");
                entity.Property(e => e.LicensePlate).HasColumnName("licenseplate");
                entity.Property(e => e.ServiceID).HasColumnName("serviceid");
                entity.Property(e => e.RewardID).HasColumnName("rewardid");
                entity.Property(e => e.PromotionID).HasColumnName("promotionid");
                entity.Property(e => e.ScheduledTime).HasColumnName("scheduledtime");
                entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>();
                entity.Property(e => e.BaseAmount).HasColumnName("baseamount");
                entity.Property(e => e.DiscountApplied).HasColumnName("discountapplied");
                entity.Property(e => e.FinalAmount).HasColumnName("finalamount");
                entity.Property(e => e.PointsEarned).HasColumnName("pointsearned");
                entity.Property(e => e.PointsRedeemed).HasColumnName("pointsredeemed");
                entity.Property(e => e.CreatedAt).HasColumnName("createdat");
                entity.Property(e => e.CompletedAt).HasColumnName("completedat");
                entity.Property(e => e.IsWalkIn).HasColumnName("iswalkin").HasDefaultValue(false);
            });

            builder.Entity<PointTransaction>(entity =>
            {
                entity.ToTable("pointtransaction");
                entity.HasKey(e => e.PointTxnID);

                entity.Property(e => e.PointTxnID).HasColumnName("pointtxnid");
                entity.Property(e => e.LoyaltyID).HasColumnName("loyaltyid");
                entity.Property(e => e.Points).HasColumnName("points");
                entity.Property(e => e.Type).HasColumnName("type").HasConversion<string>();
                entity.Property(e => e.RefBookingID).HasColumnName("refbookingid");
                entity.Property(e => e.CreatedAt).HasColumnName("createdat");
            });

            builder.Entity<Service>(entity =>
            {
                entity.ToTable("service");
                entity.HasKey(e => e.ServiceID);

                entity.Property(e => e.ServiceID).HasColumnName("serviceid");
                entity.Property(e => e.ServiceName).HasColumnName("servicename");
                entity.Property(e => e.ServiceCategory).HasColumnName("servicecategory");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.Price).HasColumnName("price");
                entity.Property(e => e.Duration).HasColumnName("duration");
                entity.Property(e => e.Status).HasColumnName("status");

                // Bảng service hiện tại không có cột createdat
            });


            builder.Entity<RewardsCatalog>(entity =>
            {
                entity.ToTable("rewards_catalog", t => t.HasCheckConstraint("CK_RewardsCatalog_DiscountType", "discounttype IN ('Fixed_Amount','Percentage')"));
                entity.HasKey(e => e.RewardID);

                entity.Property(e => e.RewardID).HasColumnName("rewardid");
                entity.Property(e => e.RewardName).HasColumnName("rewardname");
                entity.Property(e => e.PointsRequired).HasColumnName("pointsrequired");
                entity.Property(e => e.DiscountAmount).HasColumnName("discountvalue");
                entity.Property(e => e.DiscountType).HasColumnName("discounttype");
                entity.Property(e => e.IsActive).HasColumnName("isactive");

                entity.Ignore(e => e.Description);
                entity.Ignore(e => e.CreatedAt);
            });

            builder.Entity<Transaction>(entity =>
            {
                entity.ToTable("transaction");
                entity.HasKey(e => e.TransactionID);

                entity.Property(e => e.TransactionID).HasColumnName("transactionid");
                entity.Property(e => e.BookingID).HasColumnName("bookingid");
                entity.Property(e => e.Amount).HasColumnName("amount");
                entity.Property(e => e.PaymentMethod).HasColumnName("paymentmethod").HasConversion<string>();
                entity.Property(e => e.PaidAt).HasColumnName("paidat");
                entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>();

                entity.HasIndex(e => e.BookingID)
                    .HasDatabaseName("idx_txn_booking_paid")
                    .HasFilter("\"status\" = 'Paid'")
                    .IsUnique();
            });

            builder.Entity<Promotion>(entity =>
            {
                entity.ToTable("promotion", t => t.HasCheckConstraint("CK_Promotion_DiscountType", "discounttype IN ('Percentage','Fixed_Amount')"));
                entity.HasKey(e => e.PromotionID);

                entity.Property(e => e.PromotionID).HasColumnName("promotionid");
                entity.Property(e => e.Title).HasColumnName("title");
                entity.Property(e => e.PromoCode).HasColumnName("promocode");
                entity.Property(e => e.MinTierID).HasColumnName("mintierid");
                entity.Property(e => e.DiscountType).HasColumnName("discounttype");
                entity.Property(e => e.DiscountValue).HasColumnName("discountvalue");
                entity.Property(e => e.MaxUsage).HasColumnName("maxusage");
                entity.Property(e => e.StartDate).HasColumnName("startdate");
                entity.Property(e => e.EndDate).HasColumnName("enddate");
                entity.Property(e => e.IsActive).HasColumnName("isactive");
                entity.Property(e => e.MinOrderValue).HasColumnName("minordervalue");
                entity.Property(e => e.MaxDiscountAmount).HasColumnName("maxdiscountamount");

                entity.HasIndex(e => e.PromoCode).IsUnique();
            });

            builder.Entity<CustomerPromotion>(entity =>
            {
                entity.ToTable("customerpromotion");
                entity.HasKey(e => e.ID);

                entity.Property(e => e.ID).HasColumnName("id");
                entity.Property(e => e.CustomerID).HasColumnName("customerid");
                entity.Property(e => e.PromotionID).HasColumnName("promotionid");
                entity.Property(e => e.BookingID).HasColumnName("bookingid");
                entity.Property(e => e.UsedAt).HasColumnName("usedat");
                entity.Property(e => e.DiscountAmountActual).HasColumnName("discountamountactual");

                entity.HasIndex(e => e.PromotionID);
                entity.HasIndex(e => e.BookingID);
            });
        }
    }
}