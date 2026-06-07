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

        public DbSet<Booking> Bookings { get; set; }
        public DbSet<LoyaltyAccount> LoyaltyAccounts { get; set; }
        public DbSet<PointTransaction> PointTransactions { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<RewardsCatalog> RewardsCatalog { get; set; }

        // Ép Entity Framework lưu Enum dưới dạng chuỗi (String) thay vì số (Int)
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Chốt cứng tên bảng: Số ít và chữ thường (Đúng chuẩn PostgreSQL)
            builder.Entity<Booking>().ToTable("booking");
            builder.Entity<LoyaltyAccount>().ToTable("loyaltyaccount");
            builder.Entity<PointTransaction>().ToTable("pointtransaction");
            builder.Entity<Service>().ToTable("service");
            builder.Entity<RewardsCatalog>().ToTable("rewardscatalog");

            // Khai báo Khóa chính
            builder.Entity<Booking>().HasKey(b => b.BookingID);
            builder.Entity<LoyaltyAccount>().HasKey(l => l.LoyaltyID);
            builder.Entity<PointTransaction>().HasKey(p => p.PointTxnID);
            builder.Entity<Service>().HasKey(s => s.ServiceID);
            builder.Entity<RewardsCatalog>().HasKey(r => r.RewardID);

            // Ép kiểu Enum
            builder.Entity<Booking>().Property(b => b.Status).HasConversion<string>();
            builder.Entity<PointTransaction>().Property(p => p.Type).HasConversion<string>();
        }
    }
}