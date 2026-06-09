using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Enums;

namespace AutoWash.Application.Services
{
    public class TierService : ITierService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<TierService> _logger;

        public TierService(IApplicationDbContext context, ILogger<TierService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<TierResponse>> GetAllTiersAsync()
        {
            return await _context.Tiers
                .OrderBy(t => t.PriorityScore)
                .Select(t => MapToResponse(t))
                .ToListAsync();
        }

        public async Task<TierResponse> UpdateTierAsync(int tierId, UpdateTierRequest request)
        {
            var tier = await _context.Tiers.FirstOrDefaultAsync(t => t.TierID == tierId);
            if (tier == null) throw new Exception("NOT_FOUND: Không tìm thấy tier.");

            if (request.MinSpending.HasValue) tier.MinSpending = request.MinSpending.Value;
            if (request.DiscountRate.HasValue) tier.DiscountRate = request.DiscountRate.Value;
            if (request.BookingWindowDays.HasValue) tier.BookingWindowDays = request.BookingWindowDays.Value;

            await _context.SaveChangesAsync();
            return MapToResponse(tier);
        }

        // BR-21: upgrade real-time sau mỗi Booking → Completed
        public async Task EvaluateUpgradeAsync(int customerId)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerID == customerId);
            if (customer == null) return;

            // Lấy tier cao nhất mà TotalSpending đạt được
            var eligibleTier = await _context.Tiers
                .Where(t => t.MinSpending <= customer.TotalSpending)
                .OrderByDescending(t => t.MinSpending)
                .FirstOrDefaultAsync();

            if (eligibleTier == null || eligibleTier.TierID == customer.TierID) return;

            // Chỉ upgrade, không downgrade ở đây (BR-21)
            if (eligibleTier.TierID <= customer.TierID) return;

            _logger.LogInformation("[TierUpgrade] Customer {Id}: Tier {Old} → {New}",
                customerId, customer.TierID, eligibleTier.TierID);

            customer.TierID = eligibleTier.TierID;
            await _context.SaveChangesAsync();
        }

        // BR-22: downgrade mùng 1 hàng tháng, dựa trên 12 tháng gần nhất
        public async Task RunMonthlyDowngradeAsync()
        {
            var cutoff = DateTime.UtcNow.AddMonths(-12);
            var memberTier = await _context.Tiers.FirstOrDefaultAsync(t => t.TierID == 1);
            if (memberTier == null) return;

            var customers = await _context.Customers.ToListAsync();

            foreach (var customer in customers)
            {
                var recentSpending = await _context.Bookings
                    .Where(b => b.CustomerID == customer.CustomerID
                             && b.Status == BookingStatus.Completed
                             && b.CompletedAt >= cutoff)
                    .SumAsync(b => (decimal?)b.FinalAmount) ?? 0;

                var eligibleTier = await _context.Tiers
                    .Where(t => t.MinSpending <= recentSpending)
                    .OrderByDescending(t => t.MinSpending)
                    .FirstOrDefaultAsync() ?? memberTier;

                // Không downgrade dưới Member (TierID=1), không upgrade
                if (eligibleTier.TierID >= customer.TierID) continue;

                _logger.LogInformation("[TierDowngrade] Customer {Id}: Tier {Old} → {New}",
                    customer.CustomerID, customer.TierID, eligibleTier.TierID);

                customer.TierID = eligibleTier.TierID;
            }

            await _context.SaveChangesAsync();
        }

        private static TierResponse MapToResponse(Domain.Entities.Tier t) => new TierResponse
        {
            TierId = t.TierID,
            TierName = t.TierName,
            MinSpending = t.MinSpending,
            BookingWindowDays = t.BookingWindowDays,
            DiscountRate = t.DiscountRate,
            PriorityScore = t.PriorityScore
        };
    }
}
