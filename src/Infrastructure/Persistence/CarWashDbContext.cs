using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    public class CarWashDbContext : DbContext
    {
        public CarWashDbContext(DbContextOptions<CarWashDbContext> options) : base(options)
        {
        }

        public DbSet<Service> Services { get; set; } = null!;
        public DbSet<RewardsCatalog> RewardsCatalogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Service table configuration
            modelBuilder.Entity<Service>().HasKey(s => s.ServiceId);
            modelBuilder.Entity<Service>()
                .Property(s => s.ServiceName)
                .IsRequired()
                .HasMaxLength(100);
            modelBuilder.Entity<Service>()
                .Property(s => s.ServiceCategory)
                .IsRequired()
                .HasMaxLength(50);
            modelBuilder.Entity<Service>()
                .Property(s => s.Description)
                .IsRequired()
                .HasMaxLength(500);
            modelBuilder.Entity<Service>()
                .Property(s => s.Status)
                .HasDefaultValue("Active")
                .HasMaxLength(20);

            // RewardsCatalog table configuration
            modelBuilder.Entity<RewardsCatalog>().HasKey(r => r.RewardId);
            modelBuilder.Entity<RewardsCatalog>()
                .Property(r => r.RewardName)
                .IsRequired()
                .HasMaxLength(100);
            modelBuilder.Entity<RewardsCatalog>()
                .Property(r => r.Description)
                .HasMaxLength(500);

            // Seed data
            modelBuilder.Entity<Service>().HasData(
                new Service { ServiceId = 1, ServiceName = "Rửa xe cơ bản", ServiceCategory = "Basic", Description = "Rửa ngoài, sấy khô, lau kính", Price = 80000, Duration = 20, Status = "Active" },
                new Service { ServiceId = 2, ServiceName = "Rửa xe nội thất", ServiceCategory = "Interior", Description = "Vệ sinh nội thất xe", Price = 120000, Duration = 30, Status = "Active" },
                new Service { ServiceId = 3, ServiceName = "Rửa nhanh 10 phút", ServiceCategory = "Express", Description = "Rửa nhanh gọn", Price = 50000, Duration = 10, Status = "Active" }
            );

            modelBuilder.Entity<RewardsCatalog>().HasData(
                new RewardsCatalog { RewardId = 1, RewardName = "Giảm 10%", Description = "Giảm 10% cho lần rửa tiếp theo", Points = 100, IsActive = true },
                new RewardsCatalog { RewardId = 2, RewardName = "Giảm 20%", Description = "Giảm 20% cho lần rửa tiếp theo", Points = 200, IsActive = true }
            );
        }
    }
}
