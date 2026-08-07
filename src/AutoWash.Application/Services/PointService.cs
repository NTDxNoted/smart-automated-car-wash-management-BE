using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Entities;
using AutoWash.Domain.Enums;

namespace AutoWash.Application.Services
{
    public class PointService : IPointService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<PointService> _logger;

        public PointService(IApplicationDbContext context, ILogger<PointService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // BR-53 (ISSUE-09): PointsEarned = MIN(FLOOR(FinalAmount/10000), 500)
        public async Task<int> EarnPointsAsync(int bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) return 0;

            // Khách vãng lai (Password == "WALKIN", tạo bởi AdminBookingService) và tài khoản Guest
            // dùng chung cho booking công khai không đăng nhập (Password == "GUEST") không có tài
            // khoản/loyalty tier thật — không cộng điểm tích lũy cho các booking gắn với 2 loại này.
            var customer = await _context.Customers.FindAsync(booking.CustomerID);
            if (customer != null && (customer.Password == "WALKIN" || customer.Password == "GUEST"))
            {
                booking.PointsEarned = 0;
                await _context.SaveChangesAsync();
                return 0;
            }

            int calculatedPoints = (int)Math.Min(Math.Floor(booking.FinalAmount / 10000), 500);
            booking.PointsEarned = calculatedPoints;

            if (calculatedPoints > 0)
            {
                var loyalty = await _context.LoyaltyAccounts
                    .FirstOrDefaultAsync(l => l.CustomerID == booking.CustomerID);

                if (loyalty == null)
                {
                    loyalty = new LoyaltyAccount
                    {
                        CustomerID = booking.CustomerID,
                        TotalPoints = 0,
                        LastUpdated = DateTime.UtcNow
                    };
                    await _context.LoyaltyAccounts.AddAsync(loyalty);
                    await _context.SaveChangesAsync();
                }

                loyalty.TotalPoints += calculatedPoints;
                loyalty.LastUpdated = DateTime.UtcNow;

                _context.PointTransactions.Add(new PointTransaction
                {
                    LoyaltyID = loyalty.LoyaltyID,
                    Points = calculatedPoints,
                    Type = PointTransactionType.Earn,
                    RefBookingID = booking.BookingID,
                    ExpiredAt = DateTime.UtcNow.AddMonths(12),
                    CreatedAt = DateTime.UtcNow
                });

                _logger.LogInformation("[EarnPoints] BookingID={Bid} +{Pts} points", bookingId, calculatedPoints);

                // Không thể trông chờ caller SaveChanges() hộ — nếu không service này gọi đứng một mình
                // (ngoài PaymentService) sẽ mất điểm và PointTransaction âm thầm không được lưu.
                await _context.SaveChangesAsync();
            }

            return calculatedPoints;
        }

        // GET /api/loyalty (BR-59)
        public async Task<LoyaltyWalletResponse> GetWalletAsync(int customerId)
        {
            var account = await _context.LoyaltyAccounts
                .FirstOrDefaultAsync(a => a.CustomerID == customerId);

            if (account == null)
                return new LoyaltyWalletResponse
                {
                    CustomerId = customerId,
                    TotalPoints = 0,
                    CanRedeem = false,
                    Batches = new List<PointBatchDto>()
                };

            var now = DateTime.UtcNow;

            // Tổng điểm đã tiêu (Redeem + Expire đều lưu âm)
            var totalConsumed = Math.Abs(
                await _context.PointTransactions
                    .Where(t => t.LoyaltyID == account.LoyaltyID
                             && (t.Type == PointTransactionType.Redeem || t.Type == PointTransactionType.Expire))
                    .SumAsync(t => (int?)t.Points) ?? 0
            );

            // Tất cả Earn records theo thứ tự cũ → mới (FIFO)
            var earnRecords = await _context.PointTransactions
                .Where(t => t.LoyaltyID == account.LoyaltyID && t.Type == PointTransactionType.Earn)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();

            // FIFO walk: trừ cuốn chiếu, chỉ giữ batch còn dương và chưa hết hạn
            var batches = new List<PointBatchDto>();
            var remaining = totalConsumed;
            foreach (var earn in earnRecords)
            {
                if (remaining >= earn.Points)
                {
                    remaining -= earn.Points;
                    continue;
                }

                var batchPoints = earn.Points - remaining;
                remaining = 0;

                if (earn.ExpiredAt.HasValue && earn.ExpiredAt.Value > now)
                {
                    batches.Add(new PointBatchDto
                    {
                        Points = batchPoints,
                        EarnedAt = earn.CreatedAt,
                        ExpiredAt = earn.ExpiredAt.Value,
                        DaysUntilExpiry = (int)(earn.ExpiredAt.Value - now).TotalDays
                    });
                }
            }

            return new LoyaltyWalletResponse
            {
                CustomerId = customerId,
                TotalPoints = account.TotalPoints,
                CanRedeem = account.TotalPoints >= 50,
                Batches = batches
            };
        }

        // GET /api/loyalty/history
        public async Task<IEnumerable<PointHistoryResponse>> GetHistoryAsync(int customerId)
        {
            var account = await _context.LoyaltyAccounts
                .FirstOrDefaultAsync(a => a.CustomerID == customerId);

            if (account == null) return new List<PointHistoryResponse>();

            return await _context.PointTransactions
                .Where(t => t.LoyaltyID == account.LoyaltyID)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new PointHistoryResponse
                {
                    TxnId = t.PointTxnID,
                    Type = t.Type.ToString(),
                    Points = t.Points,
                    RefBookingId = t.RefBookingID,
                    CreatedAt = t.CreatedAt,
                    ExpiredAt = t.ExpiredAt
                })
                .ToListAsync();
        }

        // GET /api/loyalty/simulate (BR-60)
        public async Task<RedeemSimulateResponse> SimulateRedemptionAsync(int customerId, int rewardId, decimal baseAmount)
        {
            var reward = await _context.RewardsCatalog
                .FirstOrDefaultAsync(r => r.RewardID == rewardId && r.IsActive);

            if (reward == null)
                throw new Exception("NOT_FOUND: Không tìm thấy reward hoặc reward không còn active.");

            var account = await _context.LoyaltyAccounts
                .FirstOrDefaultAsync(a => a.CustomerID == customerId);

            var totalPoints = account?.TotalPoints ?? 0;

            // Anti double-spend (BR-59): trừ điểm đang bị lock bởi các Pending booking song song
            var lockedPoints = await _context.Bookings
                .Where(b => b.CustomerID == customerId
                         && b.Status == BookingStatus.Pending
                         && b.PointsRedeemed > 0)
                .SumAsync(b => (int?)b.PointsRedeemed) ?? 0;

            var availablePoints = totalPoints - lockedPoints;

            // BR-Rewards: % tính trên giá gốc dịch vụ; Fixed_Amount giữ nguyên số tiền.
            decimal rawDiscount = reward.DiscountType == "Percentage"
                ? Math.Round(baseAmount * (reward.DiscountAmount / 100m), 0)
                : reward.DiscountAmount;

            // BR-60: điểm thưởng chỉ được thanh toán tối đa 50% giá trị hóa đơn.
            var maxAllowed = Math.Round(baseAmount * 0.5m, 2);
            var discountApplied = Math.Min(rawDiscount, maxAllowed);

            return new RedeemSimulateResponse
            {
                RewardId = reward.RewardID,
                RewardName = reward.RewardName,
                PointsRequired = reward.PointsRequired,
                DiscountValue = reward.DiscountAmount,
                DiscountType = reward.DiscountType,
                MaxAllowed = maxAllowed,
                DiscountApplied = discountApplied,
                FinalAmount = baseAmount - discountApplied,
                IsValid = availablePoints >= reward.PointsRequired
            };
        }

        // BR-57: expire điểm hàng ngày, idempotent
        public async Task RunDailyExpiryAsync()
        {
            var now = DateTime.UtcNow;

            var affectedIds = await _context.PointTransactions
                .Where(t => t.Type == PointTransactionType.Earn && t.ExpiredAt <= now)
                .Select(t => t.LoyaltyID)
                .Distinct()
                .ToListAsync();

            foreach (var loyaltyId in affectedIds)
            {
                var totalExpiredEarns = await _context.PointTransactions
                    .Where(t => t.LoyaltyID == loyaltyId
                             && t.Type == PointTransactionType.Earn
                             && t.ExpiredAt <= now)
                    .SumAsync(t => (int?)t.Points) ?? 0;

                var alreadyExpired = Math.Abs(
                    await _context.PointTransactions
                        .Where(t => t.LoyaltyID == loyaltyId && t.Type == PointTransactionType.Expire)
                        .SumAsync(t => (int?)t.Points) ?? 0);

                var toExpire = totalExpiredEarns - alreadyExpired;
                if (toExpire <= 0) continue;

                var account = await _context.LoyaltyAccounts
                    .FirstOrDefaultAsync(a => a.LoyaltyID == loyaltyId);
                if (account == null) continue;

                _context.PointTransactions.Add(new PointTransaction
                {
                    LoyaltyID = loyaltyId,
                    Points = -toExpire,
                    Type = PointTransactionType.Expire,
                    CreatedAt = now
                });

                account.TotalPoints = Math.Max(0, account.TotalPoints - toExpire);
                account.LastUpdated = now;

                _logger.LogInformation("[PointExpiry] LoyaltyID={Id} -{Pts} points expired", loyaltyId, toExpire);
            }

            await _context.SaveChangesAsync();
        }
    }
}
