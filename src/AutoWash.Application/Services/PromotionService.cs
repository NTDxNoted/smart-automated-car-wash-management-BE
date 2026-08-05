using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Entities;
using AutoWash.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoWash.Application.Services
{
    public class PromotionService : IPromotionService
    {
        private readonly IApplicationDbContext _dbContext;

        public PromotionService(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<PromoValidateResponse> ValidatePromoAsync(string code, int? customerId)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("PROMO_EXPIRED");

            var promo = await _dbContext.Promotions
                .FirstOrDefaultAsync(p => p.PromoCode.ToLower() == code.Trim().ToLower());

            // 1. Validate Exist & Active & Date range
            if (promo == null || !promo.IsActive)
                throw new InvalidOperationException("PROMO_EXPIRED");

            var localToday = DateTime.UtcNow.AddHours(7).Date;
            if (localToday < promo.StartDate.Date || localToday > promo.EndDate.Date)
                throw new InvalidOperationException("PROMO_EXPIRED");

            // 2. Validate Tier
            if (promo.MinTierID.HasValue)
            {
                if (!customerId.HasValue || customerId.Value <= 0)
                {
                    throw new InvalidOperationException("PROMO_MEMBER_ONLY");
                }

                var customer = await _dbContext.Customers
                    .FirstOrDefaultAsync(c => c.CustomerID == customerId.Value);

                if (customer == null)
                    throw new InvalidOperationException("PROMO_MEMBER_ONLY");

                if (customer.TierID < promo.MinTierID.Value)
                    throw new InvalidOperationException("PROMO_TIER_INSUFFICIENT");
            }

            // 3. Validate Max Usage (Total System Limit)
            if (promo.MaxUsage.HasValue)
            {
                var usageCount = await _dbContext.Bookings
                    .CountAsync(b => b.PromotionID == promo.PromotionID && b.Status != BookingStatus.Cancelled);

                if (usageCount >= promo.MaxUsage.Value)
                    throw new InvalidOperationException("PROMO_LIMIT_REACHED");
            }

            // 4. Validate Personal Limit (Double-Redeem prevention)
            if (customerId.HasValue && customerId.Value > 0)
            {
                var userUsageCount = await _dbContext.Bookings
                    .CountAsync(b => b.CustomerID == customerId.Value 
                                  && b.PromotionID == promo.PromotionID 
                                  && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Completed));

                if (userUsageCount >= 1)
                    throw new InvalidOperationException("PROMO_USER_ALREADY_USED");
            }

            // Fetch Tier name for display
            string? minTierName = null;
            if (promo.MinTierID.HasValue)
            {
                var tier = await _dbContext.Tiers.FirstOrDefaultAsync(t => t.TierID == promo.MinTierID.Value);
                minTierName = tier?.TierName;
            }

            return new PromoValidateResponse
            {
                PromotionId = promo.PromotionID,
                Title = promo.Title,
                PromoCode = promo.PromoCode,
                DiscountType = promo.DiscountType,
                DiscountValue = promo.DiscountValue,
                MinOrderValue = promo.MinOrderValue,
                MaxDiscountAmount = promo.MaxDiscountAmount,
                IsValid = true,
                MinTierRequired = minTierName
            };
        }

        public async Task<IEnumerable<Promotion>> GetPromotionsAsync()
        {
            return await _dbContext.Promotions
                .Include(p => p.MinTier)
                .OrderByDescending(p => p.PromotionID)
                .ToListAsync();
        }

        public async Task<Promotion> CreatePromotionAsync(CreatePromoRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var codeExists = await _dbContext.Promotions
                .AnyAsync(p => p.PromoCode.ToLower() == request.PromoCode.Trim().ToLower());

            if (codeExists)
                throw new ArgumentException("PROMO_CODE_EXISTS");

            if (request.DiscountType == "Percentage" && request.DiscountValue > 100)
                throw new ArgumentException("PROMO_INVALID_PERCENTAGE");

            var promo = new Promotion
            {
                Title = request.Title.Trim(),
                PromoCode = request.PromoCode.Trim().ToUpper(),
                MinTierID = request.MinTierID,
                DiscountType = request.DiscountType,
                DiscountValue = request.DiscountValue,
                MaxUsage = request.MaxUsage,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsActive = request.IsActive,
                MinOrderValue = request.MinOrderValue,
                MaxDiscountAmount = request.MaxDiscountAmount
            };

            _dbContext.Promotions.Add(promo);
            await _dbContext.SaveChangesAsync();

            return promo;
        }

        public async Task<Promotion> UpdatePromotionAsync(int id, UpdatePromoRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var promo = await _dbContext.Promotions
                .FirstOrDefaultAsync(p => p.PromotionID == id);

            if (promo == null)
                throw new KeyNotFoundException("PROMO_NOT_FOUND");

            if (request.DiscountType == "Percentage" && request.DiscountValue > 100)
                throw new ArgumentException("PROMO_INVALID_PERCENTAGE");

            var newCode = request.PromoCode.Trim().ToUpper();
            var codeUsedByAnotherPromo = await _dbContext.Promotions
                .AnyAsync(p => p.PromotionID != id && p.PromoCode.ToLower() == newCode.ToLower());
            if (codeUsedByAnotherPromo)
                throw new ArgumentException("PROMO_CODE_EXISTS");

            promo.Title = request.Title.Trim();
            promo.PromoCode = newCode;
            promo.MinTierID = request.MinTierID;
            promo.DiscountType = request.DiscountType;
            promo.DiscountValue = request.DiscountValue;
            promo.MaxUsage = request.MaxUsage;
            promo.StartDate = request.StartDate;
            promo.EndDate = request.EndDate;
            promo.IsActive = request.IsActive;
            promo.MinOrderValue = request.MinOrderValue;
            promo.MaxDiscountAmount = request.MaxDiscountAmount;

            await _dbContext.SaveChangesAsync();

            return promo;
        }

        public async Task<Promotion> TogglePromoActiveAsync(int id)
        {
            var promo = await _dbContext.Promotions
                .FirstOrDefaultAsync(p => p.PromotionID == id);

            if (promo == null)
                throw new KeyNotFoundException("PROMO_NOT_FOUND");

            promo.IsActive = !promo.IsActive;
            await _dbContext.SaveChangesAsync();

            return promo;
        }

        public async Task<PromoDetailResponse> GetPromoUsageAsync(int id)
        {
            var promo = await _dbContext.Promotions.FirstOrDefaultAsync(p => p.PromotionID == id);
            if (promo == null)
                throw new KeyNotFoundException("PROMO_NOT_FOUND");

            // Stats window: last 365 days, clamped to the promo's StartDate if it's younger than
            // that — never show data older than 1 year, and never pad before the promo existed.
            // Booking.CreatedAt is stored UTC; StartDate is a plain VN-local date (see
            // ValidatePromoAsync above), so shift the VN boundaries by -7h before comparing.
            var localToday = DateTime.UtcNow.AddHours(7).Date;
            var oneYearAgoLocal = localToday.AddDays(-365);
            var rangeStartLocal = promo.StartDate.Date > oneYearAgoLocal ? promo.StartDate.Date : oneYearAgoLocal;

            var rangeStartUtc = rangeStartLocal.AddHours(-7);
            var rangeEndUtc = localToday.AddDays(1).AddHours(-7);

            var rows = await _dbContext.Bookings
                .Where(b => b.PromotionID == id && b.CreatedAt >= rangeStartUtc && b.CreatedAt < rangeEndUtc)
                .Select(b => new
                {
                    b.BookingID,
                    b.CustomerID,
                    CustomerName = _dbContext.Customers.Where(c => c.CustomerID == b.CustomerID).Select(c => c.FullName).FirstOrDefault() ?? string.Empty,
                    b.Phone,
                    b.CreatedAt,
                    b.DiscountApplied,
                    b.FinalAmount
                })
                .ToListAsync();

            var totalDiscount = rows.Sum(r => r.DiscountApplied);
            var totalRevenue = rows.Sum(r => r.FinalAmount);

            // Monthly buckets span the same window used for the totals above (so the chart
            // always sums back to the summary figures), grouped by VN-local calendar month.
            var monthlyMap = new Dictionary<string, PromoMonthlyStatDto>();
            var cursor = new DateTime(rangeStartLocal.Year, rangeStartLocal.Month, 1);
            var lastMonth = new DateTime(localToday.Year, localToday.Month, 1);
            while (cursor <= lastMonth)
            {
                var key = cursor.ToString("yyyy-MM");
                monthlyMap[key] = new PromoMonthlyStatDto { Month = key, UsageCount = 0, Revenue = 0m, Discount = 0m };
                cursor = cursor.AddMonths(1);
            }

            foreach (var row in rows)
            {
                var localCreatedAt = row.CreatedAt.AddHours(7);
                var key = localCreatedAt.ToString("yyyy-MM");
                if (monthlyMap.TryGetValue(key, out var bucket))
                {
                    bucket.UsageCount++;
                    bucket.Revenue += row.FinalAmount;
                    bucket.Discount += row.DiscountApplied;
                }
            }

            return new PromoDetailResponse
            {
                PromotionId = promo.PromotionID,
                Title = promo.Title,
                PromoCode = promo.PromoCode,
                RangeStart = rangeStartLocal.ToString("yyyy-MM-dd"),
                RangeEnd = localToday.ToString("yyyy-MM-dd"),
                TotalUsageCount = rows.Count,
                TotalDiscountAmount = totalDiscount,
                TotalRevenueGenerated = totalRevenue,
                UniqueCustomerCount = rows.Select(r => r.CustomerID).Distinct().Count(),
                EffectivenessPercentage = totalDiscount == 0 ? 0m
                    : Math.Round((totalRevenue - totalDiscount) / totalDiscount * 100, 2),
                MonthlyStats = monthlyMap.Values.OrderBy(m => m.Month).ToList(),
                Usages = rows
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new PromoUsageResponse
                    {
                        BookingId = r.BookingID,
                        CustomerId = r.CustomerID,
                        CustomerName = r.CustomerName,
                        Phone = r.Phone,
                        UsedAt = r.CreatedAt,
                        DiscountAmountActual = r.DiscountApplied
                    })
                    .ToList()
            };
        }
    }
}
