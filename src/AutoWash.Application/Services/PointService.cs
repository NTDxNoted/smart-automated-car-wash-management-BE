using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Entities;
using AutoWash.Domain.Enums;

namespace AutoWash.Application.Services
{
    public class PointService : IPointService
    {
        private readonly IApplicationDbContext _context;

        public PointService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> EarnPointsAsync(int bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) return 0;

            // BR-53: PointsEarned = MIN(FLOOR(FinalAmount/10000), 500)
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
                    
                    // Lưu thay đổi tạm thời để sinh ra LoyaltyID cho PointTransaction
                    await _context.SaveChangesAsync();
                }

                loyalty.TotalPoints += calculatedPoints;
                loyalty.LastUpdated = DateTime.UtcNow;

                var pointTxn = new PointTransaction
                {
                    LoyaltyID = loyalty.LoyaltyID,
                    Points = calculatedPoints,
                    Type = PointTransactionType.Earn,
                    RefBookingID = booking.BookingID,
                    ExpiredAt = DateTime.UtcNow.AddMonths(12),
                    CreatedAt = DateTime.UtcNow
                };

                await _context.PointTransactions.AddAsync(pointTxn);
            }

            return calculatedPoints;
        }

        public async Task RedeemPointsAsync(int bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null || booking.PointsRedeemed <= 0) return;

            var loyalty = await _context.LoyaltyAccounts
                .FirstOrDefaultAsync(l => l.CustomerID == booking.CustomerID);

            if (loyalty == null)
            {
                throw new Exception("LOYALTY_ACCOUNT_NOT_FOUND: Không tìm thấy tài khoản loyalty để trừ điểm.");
            }

            loyalty.TotalPoints -= booking.PointsRedeemed;
            if (loyalty.TotalPoints < 0)
            {
                loyalty.TotalPoints = 0;
            }
            loyalty.LastUpdated = DateTime.UtcNow;

            var pointTxn = new PointTransaction
            {
                LoyaltyID = loyalty.LoyaltyID,
                Points = booking.PointsRedeemed,
                Type = PointTransactionType.Redeem,
                RefBookingID = booking.BookingID,
                ExpiredAt = null,
                CreatedAt = DateTime.UtcNow
            };

            await _context.PointTransactions.AddAsync(pointTxn);
        }
    }
}
