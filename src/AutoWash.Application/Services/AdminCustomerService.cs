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

        // --- TASK 1: LẤY DANH SÁCH ---
        public async Task<PagedResponse<CustomerAdminResponseDto>> GetCustomersAsync(string? tier, bool? isLocked, int page, int pageSize)
        {
            // Nguồn sự thật của Tier là Customer.TierID + bảng Tiers (giống CustomerService.GetProfileAsync),
            // không phải suy ra từ điểm thưởng — điểm và tier là 2 trục độc lập trong hệ thống này.
            var tierNames = await _context.Tiers.ToDictionaryAsync(t => t.TierID, t => t.TierName);

            var query = _context.Customers
                .Include(c => c.LoyaltyAccount) // Nhớ Include để lấy TotalPoints
                .Where(c => c.Role != "ADMIN") // Bảng Customers dùng chung cho cả Admin (AdminAuthService) — không lộ tài khoản Admin qua API quản lý khách hàng
                .AsQueryable();

            if (isLocked.HasValue)
            {
                query = query.Where(c => c.IsLocked == isLocked.Value);
            }

            var accounts = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            string ResolveTier(AutoWash.Domain.Entities.Customer c) =>
                tierNames.TryGetValue(c.TierID, out var name) ? name : (c.TierID == 1 ? "Member" : c.TierID.ToString());

            if (!string.IsNullOrEmpty(tier))
            {
                accounts = accounts.Where(c => ResolveTier(c).Equals(tier, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var total = accounts.Count;

            var customerIds = accounts.Select(c => c.CustomerID).ToList();
            var bookingCounts = await _context.Bookings
                .Where(b => customerIds.Contains(b.CustomerID))
                .GroupBy(b => b.CustomerID)
                .Select(g => new { CustomerID = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CustomerID, x => x.Count);

            var pagedAccounts = accounts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CustomerAdminResponseDto
                {
                    CustomerId = c.CustomerID,
                    FullName = c.FullName,
                    Phone = c.Phone,
                    Tier = ResolveTier(c),
                    Points = c.LoyaltyAccount?.TotalPoints ?? 0,
                    TotalSpending = c.TotalSpending,
                    IsLocked = c.IsLocked,
                    SuspendedUntil = c.SuspendedUntil,
                    CreatedAt = c.CreatedAt,
                    IsWalkIn = c.Password == "WALKIN",
                    BookingCount = bookingCounts.TryGetValue(c.CustomerID, out var count) ? count : 0,
                    LastVisit = c.LastVisit,
                    AdminNotes = c.AdminNotes
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

            if (customer == null || customer.Role == "ADMIN")
                throw new Exception("NOT_FOUND: Không tìm thấy hồ sơ khách hàng.");

            var tierName = await _context.Tiers
                .Where(t => t.TierID == customer.TierID)
                .Select(t => t.TierName)
                .FirstOrDefaultAsync();

            var bookings = customer.Bookings.OrderByDescending(b => b.ScheduledTime).ToList();
            var serviceIds = bookings.Select(b => b.ServiceID).Distinct().ToList();
            var services = await _context.Services
                .Where(s => serviceIds.Contains(s.ServiceID))
                .ToDictionaryAsync(s => s.ServiceID, s => s.ServiceName);

            var bookingIds = bookings.Select(b => b.BookingID).ToList();
            var transactions = await _context.Transactions
                .Where(t => bookingIds.Contains(t.BookingID))
                .ToDictionaryAsync(t => t.BookingID, t => t);

            var promotions = await _context.Promotions.ToDictionaryAsync(p => p.PromotionID, p => (p.Title, p.PromoCode));
            var rewardNames = await _context.RewardsCatalog.ToDictionaryAsync(r => r.RewardID, r => r.RewardName);

            string? ResolvePromotionApplied(AutoWash.Domain.Entities.Booking b)
            {
                if (b.PromotionID.HasValue && promotions.TryGetValue(b.PromotionID.Value, out var promo))
                    return string.IsNullOrWhiteSpace(promo.PromoCode) ? promo.Title : $"{promo.Title} ({promo.PromoCode})";
                if (b.RewardID.HasValue && rewardNames.TryGetValue(b.RewardID.Value, out var rewardName))
                    return rewardName;
                return null;
            }

            var detailDto = new CustomerDetailAdminResponseDto
            {
                CustomerId = customer.CustomerID,
                FullName = customer.FullName,
                Phone = customer.Phone,
                Tier = tierName ?? (customer.TierID == 1 ? "Member" : customer.TierID.ToString()),
                Points = customer.LoyaltyAccount?.TotalPoints ?? 0,
                TotalSpending = customer.TotalSpending,
                IsLocked = customer.IsLocked,
                SuspendedUntil = customer.SuspendedUntil,
                CreatedAt = customer.CreatedAt,
                IsWalkIn = customer.Password == "WALKIN",
                BookingCount = bookings.Count,
                LastVisit = customer.LastVisit,
                AdminNotes = customer.AdminNotes,

                BookingHistory = bookings.Select(b =>
                {
                    transactions.TryGetValue(b.BookingID, out var transaction);
                    return new CustomerBookingHistoryItemDto
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
                        },
                        PaymentMethod = transaction?.PaymentMethod.ToString(),
                        PromotionApplied = ResolvePromotionApplied(b),
                        InvoiceCode = transaction != null ? $"HD{transaction.TransactionID:D5}" : null
                    };
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

            if (customer.Role == "ADMIN")
                throw new Exception("FORBIDDEN: Không thể khóa/mở khóa tài khoản Admin qua API quản lý khách hàng.");

            customer.IsLocked = !customer.IsLocked;
            await _context.SaveChangesAsync();



            return new LockCustomerResponseDto
            {
                CustomerId = customer.CustomerID,
                IsLocked = customer.IsLocked,
                Message = customer.IsLocked ? "Tài khoản đã bị khóa." : "Tài khoản đã được mở khóa."
            };
        }

        // --- TASK 4: CẬP NHẬT GHI CHÚ ADMIN ---
        public async Task<CustomerAdminResponseDto> UpdateCustomerNotesAsync(int customerId, string? notes)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerID == customerId);

            if (customer == null || customer.Role == "ADMIN")
                throw new Exception("NOT_FOUND: Không tìm thấy khách hàng để thao tác.");

            customer.AdminNotes = notes;
            await _context.SaveChangesAsync();

            var tierName = await _context.Tiers
                .Where(t => t.TierID == customer.TierID)
                .Select(t => t.TierName)
                .FirstOrDefaultAsync();

            return new CustomerAdminResponseDto
            {
                CustomerId = customer.CustomerID,
                FullName = customer.FullName,
                Phone = customer.Phone,
                Tier = tierName ?? (customer.TierID == 1 ? "Member" : customer.TierID.ToString()),
                TotalSpending = customer.TotalSpending,
                IsLocked = customer.IsLocked,
                SuspendedUntil = customer.SuspendedUntil,
                CreatedAt = customer.CreatedAt,
                IsWalkIn = customer.Password == "WALKIN",
                LastVisit = customer.LastVisit,
                AdminNotes = customer.AdminNotes
            };
        }
    }
}