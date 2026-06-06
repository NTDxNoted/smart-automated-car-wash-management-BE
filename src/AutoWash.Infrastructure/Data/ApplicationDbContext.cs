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

        // Khai báo 3 bảng liên quan đến Issue 07
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<LoyaltyAccount> LoyaltyAccounts { get; set; }
        public DbSet<PointTransaction> PointTransactions { get; set; }

        // Ép Entity Framework lưu Enum dưới dạng chuỗi (String) thay vì số (Int)
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Chốt cứng tên bảng: Số ít và chữ thường (Đúng chuẩn PostgreSQL)
            builder.Entity<Booking>().ToTable("booking");
            builder.Entity<Customer>().ToTable("customer");
            builder.Entity<LoyaltyAccount>().ToTable("loyaltyaccount");
            builder.Entity<PointTransaction>().ToTable("pointtransaction");

            // Khai báo Khóa chính
            builder.Entity<Booking>().HasKey(b => b.BookingID);
            builder.Entity<Customer>().HasKey(c => c.CustomerID);
            builder.Entity<LoyaltyAccount>().HasKey(l => l.LoyaltyID);
            builder.Entity<PointTransaction>().HasKey(p => p.PointTxnID);

            builder.Entity<Customer>()
                .HasIndex(c => c.Phone)
                .IsUnique();

            // Ép kiểu Enum
            builder.Entity<Booking>().Property(b => b.Status).HasConversion<string>();
            builder.Entity<PointTransaction>().Property(p => p.Type).HasConversion<string>();
        }
    }
}