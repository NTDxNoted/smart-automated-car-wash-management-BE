using AutoWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoWash.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        // TOCTOU (BR-19): khóa advisory theo ngày để chỉ 1 request tạo booking cho ngày đó được xử lý
        // tại một thời điểm, tránh 2 request đọc overlapCount giống nhau rồi cùng insert vượt
        // MaxParallelSlots. Chỉ có tác dụng trên Postgres (Infrastructure); provider khác (vd. InMemory
        // trong unit test) coi như no-op.
        Task AcquireBookingDateLockAsync(DateTime date);

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
        DbSet<CustomerNotification> CustomerNotifications { get; set; }
        DbSet<VwCustomerRfm> VwCustomerRfm { get; set; }
        DbSet<EmailOtp> EmailOtps { get; set; }
        DbSet<GuestEmailOtp> GuestEmailOtps { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}