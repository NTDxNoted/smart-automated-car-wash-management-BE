using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;

namespace AutoWash.Application.Services
{
    public class AdminCustomerService : IAdminCustomerService
    {
        private readonly IApplicationDbContext _context;

        public AdminCustomerService(IApplicationDbContext context)
        {
            _context = context;
        }

        // --- Hàm hỗ trợ tính toán Tier ---
        private string DetermineTier(int totalPoints)
        {
            if (totalPoints >= 5000) return "Platinum";
            if (totalPoints >= 2000) return "Gold";
            if (totalPoints >= 500) return "Silver";
            return "Member";
        }

        // --- TASK 1: LẤY DANH SÁCH ---
        public async Task<PagedResponse<CustomerAdminResponseDto>> GetCustomersAsync(string? tier, bool? isLocked, int page, int pageSize)
        {
            var query = _context.Customers
                .Include(c => c.LoyaltyAccount) // Nhớ Include để lấy TotalPoints
                .AsQueryable();

            if (isLocked.HasValue)
            {
                query = query.Where(c => c.IsLocked == isLocked.Value);
            }

            var accounts = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            if (!string.IsNullOrEmpty(tier))
            {
                accounts = accounts.Where(c =>
                    (c.LoyaltyAccount != null ? DetermineTier(c.LoyaltyAccount.TotalPoints) : "Member").ToLower() == tier.ToLower()
                ).ToList();
            }

            var total = accounts.Count;

            var pagedAccounts = accounts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CustomerAdminResponseDto
                {
                    CustomerId = c.CustomerID,
                    FullName = c.FullName,
                    Phone = c.Phone,
                    Tier = c.LoyaltyAccount != null ? DetermineTier(c.LoyaltyAccount.TotalPoints) : "Member",
                    TotalSpending = c.TotalSpending,
                    IsLocked = c.IsLocked,
                    SuspendedUntil = c.SuspendedUntil,
                    CreatedAt = c.CreatedAt
                })
                .ToList();

            return new PagedResponse<CustomerAdminResponseDto>
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Data = pagedAccounts
            };
        }

        // --- TASK 2: LẤY CHI TIẾT ---
        public async Task<CustomerDetailAdminResponseDto> GetCustomerByIdAsync(int customerId)
        {
            var customer = await _context.Customers
                .Include(c => c.LoyaltyAccount)
                .Include(c => c.Bookings)
                .FirstOrDefaultAsync(c => c.CustomerID == customerId);

            if (customer == null)
                throw new Exception("NOT_FOUND: Không tìm thấy hồ sơ khách hàng.");

            var bookings = customer.Bookings.OrderByDescending(b => b.ScheduledTime).ToList();
            var serviceIds = bookings.Select(b => b.ServiceID).Distinct().ToList();
            var services = await _context.Services
                .Where(s => serviceIds.Contains(s.ServiceID))
                .ToDictionaryAsync(s => s.ServiceID, s => s.ServiceName);

            var detailDto = new CustomerDetailAdminResponseDto
            {
                CustomerId = customer.CustomerID,
                FullName = customer.FullName,
                Phone = customer.Phone,
                Tier = customer.LoyaltyAccount != null ? DetermineTier(customer.LoyaltyAccount.TotalPoints) : "Member",
                TotalSpending = customer.TotalSpending,
                IsLocked = customer.IsLocked,
                SuspendedUntil = customer.SuspendedUntil,
                CreatedAt = customer.CreatedAt,

                BookingHistory = bookings.Select(b => new BookingResponseDto
                {
                    BookingId = b.BookingID,
                    LicensePlate = b.LicensePlate,
                    ScheduledTime = b.ScheduledTime,
                    Status = b.Status.ToString(),
                    FinalAmount = b.FinalAmount,
                    PointsEarned = b.PointsEarned,
                    Service = new ServiceResponse
                    {
                        ServiceId = b.ServiceID,
                        ServiceName = services.TryGetValue(b.ServiceID, out var name) ? name : "Dịch vụ không xác định"
                    }
                }).ToList()
            };

            return detailDto;
        }

        // --- TASK 3: KHÓA / MỞ KHÓA TÀI KHOẢN ---
        public async Task<LockCustomerResponseDto> ToggleLockCustomerAsync(int customerId)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerID == customerId);

            if (customer == null)
                throw new Exception("NOT_FOUND: Không tìm thấy khách hàng để thao tác.");

            customer.IsLocked = !customer.IsLocked;
            await _context.SaveChangesAsync();

            

            return new LockCustomerResponseDto
            {
                CustomerId = customer.CustomerID,
                IsLocked = customer.IsLocked,
                Message = customer.IsLocked ? "Tài khoản đã bị khóa." : "Tài khoản đã được mở khóa."
            };
        }
    }
}