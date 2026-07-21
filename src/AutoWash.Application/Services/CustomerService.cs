using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;

namespace AutoWash.Application.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly IApplicationDbContext _context;
        private readonly ITierService _tierService;

        public CustomerService(IApplicationDbContext context, ITierService tierService)
        {
            _context = context;
            _tierService = tierService;
        }

        public async Task<ProfileResponse> GetProfileAsync(int customerId)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerID == customerId);
            if (customer == null) throw new Exception("NOT_FOUND: Không tìm thấy tài khoản.");

            // BR-21: quét nâng hạng ngay khi khách xem Profile, phòng trường hợp TotalSpending
            // đã đủ điều kiện tier cao hơn nhưng chưa có sự kiện Completed nào kích hoạt lại
            // (vd. dữ liệu được chỉnh sửa trực tiếp, hoặc job đánh giá bị bỏ lỡ trước đó).
            await _tierService.EvaluateUpgradeAsync(customerId);

            var loyalty = await _context.LoyaltyAccounts.FirstOrDefaultAsync(l => l.CustomerID == customerId);

            // Customer.Tier là [NotMapped] (luôn "1" sau khi load lại từ DB) — TierID mới là
            // nguồn sự thật, phải resolve tên qua bảng Tiers giống AuthService.
            var tierName = await _context.Tiers
                .Where(t => t.TierID == customer.TierID)
                .Select(t => t.TierName)
                .FirstOrDefaultAsync();

            return new ProfileResponse
            {
                CustomerId = customer.CustomerID,
                FullName = customer.FullName,
                Phone = customer.Phone,
                Tier = tierName ?? (customer.TierID == 1 ? "Member" : customer.TierID.ToString()),
                TotalSpending = customer.TotalSpending,
                LoyaltyPoints = loyalty?.TotalPoints ?? 0,
                LastVisit = customer.LastVisit,
                SuspendedUntil = customer.SuspendedUntil
            };
        }

        public async Task<ProfileResponse> UpdateProfileAsync(int customerId, UpdateProfileRequest request)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerID == customerId);
            if (customer == null) throw new Exception("NOT_FOUND: Không tìm thấy tài khoản.");

            if (!string.IsNullOrWhiteSpace(request.FullName))
                customer.FullName = request.FullName;

            await _context.SaveChangesAsync();
            return await GetProfileAsync(customerId);
        }
    }
}
