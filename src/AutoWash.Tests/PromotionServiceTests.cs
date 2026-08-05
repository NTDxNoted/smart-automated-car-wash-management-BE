using Xunit;
using Microsoft.EntityFrameworkCore;
using AutoWash.Application.DTOs;
using AutoWash.Application.Services;
using AutoWash.Domain.Entities;
using AutoWash.Domain.Enums;
using AutoWash.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoWash.Tests.Application.Services
{
    public class PromotionServiceTests
    {
        private ApplicationDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task ValidatePromoAsync_WithValidPromoAndGuest_ShouldReturnValidateResponse()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var service = new PromotionService(dbContext);

            var promo = new Promotion
            {
                PromotionID = 1,
                Title = "Welcome Promo",
                PromoCode = "WELCOME2025",
                DiscountType = "Fixed_Amount",
                DiscountValue = 30000m,
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddDays(5),
                IsActive = true,
                MinOrderValue = 50000m,
                MaxDiscountAmount = null
            };

            dbContext.Promotions.Add(promo);
            await dbContext.SaveChangesAsync();

            // Act
            var result = await service.ValidatePromoAsync("WELCOME2025", null);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsValid);
            Assert.Equal("WELCOME2025", result.PromoCode);
            Assert.Equal("Fixed_Amount", result.DiscountType);
            Assert.Equal(30000m, result.DiscountValue);
        }

        [Fact]
        public async Task ValidatePromoAsync_WithExpiredPromo_ShouldThrowPROMO_EXPIRED()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var service = new PromotionService(dbContext);

            var promo = new Promotion
            {
                PromotionID = 1,
                Title = "Expired Promo",
                PromoCode = "EXPIRED10",
                DiscountType = "Percentage",
                DiscountValue = 10m,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(-2), // Expired
                IsActive = true
            };

            dbContext.Promotions.Add(promo);
            await dbContext.SaveChangesAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ValidatePromoAsync("EXPIRED10", null));
            Assert.Equal("PROMO_EXPIRED", exception.Message);
        }

        [Fact]
        public async Task ValidatePromoAsync_WithTierPromoAndGuest_ShouldThrowPROMO_MEMBER_ONLY()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var service = new PromotionService(dbContext);

            var promo = new Promotion
            {
                PromotionID = 1,
                Title = "Silver Promo",
                PromoCode = "SILVER05",
                DiscountType = "Percentage",
                DiscountValue = 5m,
                MinTierID = 2, // Requires Silver (TierID = 2)
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddDays(5),
                IsActive = true
            };

            dbContext.Promotions.Add(promo);
            await dbContext.SaveChangesAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ValidatePromoAsync("SILVER05", null)); // Guest has no CustomerId
            Assert.Equal("PROMO_MEMBER_ONLY", exception.Message);
        }

        [Fact]
        public async Task ValidatePromoAsync_WithInsufficientTier_ShouldThrowPROMO_TIER_INSUFFICIENT()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var service = new PromotionService(dbContext);

            var customer = new Customer
            {
                CustomerID = 1,
                FullName = "Normal Member",
                TierID = 1 // Member Tier (TierID = 1)
            };

            var promo = new Promotion
            {
                PromotionID = 1,
                Title = "Gold Promo",
                PromoCode = "GOLDDEAL",
                DiscountType = "Percentage",
                DiscountValue = 15m,
                MinTierID = 3, // Requires Gold (TierID = 3)
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddDays(5),
                IsActive = true
            };

            dbContext.Customers.Add(customer);
            dbContext.Promotions.Add(promo);
            await dbContext.SaveChangesAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ValidatePromoAsync("GOLDDEAL", customer.CustomerID));
            Assert.Equal("PROMO_TIER_INSUFFICIENT", exception.Message);
        }

        [Fact]
        public async Task ValidatePromoAsync_WithMaxUsageReached_ShouldThrowPROMO_LIMIT_REACHED()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var service = new PromotionService(dbContext);

            var promo = new Promotion
            {
                PromotionID = 1,
                Title = "Limited Promo",
                PromoCode = "LIMIT10",
                DiscountType = "Fixed_Amount",
                DiscountValue = 10000m,
                MaxUsage = 2, // Max 2 usages
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddDays(5),
                IsActive = true
            };

            var booking1 = new Booking
            {
                BookingID = 101,
                CustomerID = 1,
                PromotionID = 1,
                Status = BookingStatus.Completed
            };

            var booking2 = new Booking
            {
                BookingID = 102,
                CustomerID = 2,
                PromotionID = 1,
                Status = BookingStatus.Pending
            };

            dbContext.Promotions.Add(promo);
            dbContext.Bookings.AddRange(booking1, booking2);
            await dbContext.SaveChangesAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ValidatePromoAsync("LIMIT10", 3));
            Assert.Equal("PROMO_LIMIT_REACHED", exception.Message);
        }

        [Fact]
        public async Task ValidatePromoAsync_WithUserAlreadyUsed_ShouldThrowPROMO_USER_ALREADY_USED()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var service = new PromotionService(dbContext);

            var customer = new Customer
            {
                CustomerID = 1,
                FullName = "Normal Member",
                TierID = 1
            };

            var promo = new Promotion
            {
                PromotionID = 1,
                Title = "One Time Promo",
                PromoCode = "ONETIME",
                DiscountType = "Fixed_Amount",
                DiscountValue = 10000m,
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddDays(5),
                IsActive = true
            };

            var booking = new Booking
            {
                BookingID = 101,
                CustomerID = 1,
                PromotionID = 1,
                Status = BookingStatus.Completed // Already used
            };

            dbContext.Customers.Add(customer);
            dbContext.Promotions.Add(promo);
            dbContext.Bookings.Add(booking);
            await dbContext.SaveChangesAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ValidatePromoAsync("ONETIME", customer.CustomerID));
            Assert.Equal("PROMO_USER_ALREADY_USED", exception.Message);
        }

        [Fact]
        public async Task CreatePromotionAsync_WithDuplicateCode_ShouldThrowPROMO_CODE_EXISTS()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var service = new PromotionService(dbContext);

            var promo = new Promotion
            {
                PromotionID = 1,
                Title = "Existing Promo",
                PromoCode = "DUPLICATE",
                DiscountType = "Percentage",
                DiscountValue = 10m,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(5),
                IsActive = true
            };

            dbContext.Promotions.Add(promo);
            await dbContext.SaveChangesAsync();

            var request = new CreatePromoRequest
            {
                Title = "New Promo",
                PromoCode = "DUPLICATE",
                DiscountType = "Percentage",
                DiscountValue = 15m,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(5),
                IsActive = true
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => service.CreatePromotionAsync(request));
            Assert.Equal("PROMO_CODE_EXISTS", exception.Message);
        }

        [Fact]
        public async Task CreatePromotionAsync_WithPercentageOver100_ShouldThrowPROMO_INVALID_PERCENTAGE()
        {
            var dbContext = CreateDbContext();
            var service = new PromotionService(dbContext);

            var request = new CreatePromoRequest
            {
                Title = "Too Generous Promo",
                PromoCode = "FREE200",
                DiscountType = "Percentage",
                DiscountValue = 200m,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(5),
                IsActive = true
            };

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => service.CreatePromotionAsync(request));
            Assert.Equal("PROMO_INVALID_PERCENTAGE", exception.Message);
            Assert.Equal(0, await dbContext.Promotions.CountAsync());
        }

        [Fact]
        public async Task UpdatePromotionAsync_WithPercentageOver100_ShouldThrowPROMO_INVALID_PERCENTAGE()
        {
            var dbContext = CreateDbContext();
            var service = new PromotionService(dbContext);

            var promo = new Promotion
            {
                PromotionID = 1,
                Title = "Existing Promo",
                PromoCode = "EXIST01",
                DiscountType = "Percentage",
                DiscountValue = 10m,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(5),
                IsActive = true
            };
            dbContext.Promotions.Add(promo);
            await dbContext.SaveChangesAsync();

            var request = new UpdatePromoRequest
            {
                Title = "Existing Promo",
                PromoCode = "EXIST01",
                DiscountType = "Percentage",
                DiscountValue = 150m,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(5),
                IsActive = true
            };

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => service.UpdatePromotionAsync(promo.PromotionID, request));
            Assert.Equal("PROMO_INVALID_PERCENTAGE", exception.Message);
            Assert.Equal(10m, promo.DiscountValue); // không bị thay đổi khi request không hợp lệ
        }

        [Fact]
        public async Task UpdatePromotionAsync_WithNewPromoCode_ShouldPersistPromoCode()
        {
            var dbContext = CreateDbContext();
            var service = new PromotionService(dbContext);

            var promo = new Promotion
            {
                PromotionID = 1,
                Title = "Existing Promo",
                PromoCode = "OLDCODE",
                DiscountType = "Percentage",
                DiscountValue = 10m,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(5),
                IsActive = true
            };
            dbContext.Promotions.Add(promo);
            await dbContext.SaveChangesAsync();

            var request = new UpdatePromoRequest
            {
                Title = "Existing Promo",
                PromoCode = "newcode",
                DiscountType = "Percentage",
                DiscountValue = 10m,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(5),
                IsActive = true
            };

            var result = await service.UpdatePromotionAsync(promo.PromotionID, request);

            Assert.Equal("NEWCODE", result.PromoCode);
        }

        [Fact]
        public async Task UpdatePromotionAsync_WithPromoCodeUsedByAnotherPromo_ShouldThrowPROMO_CODE_EXISTS()
        {
            var dbContext = CreateDbContext();
            var service = new PromotionService(dbContext);

            dbContext.Promotions.AddRange(
                new Promotion
                {
                    PromotionID = 1,
                    Title = "Promo A",
                    PromoCode = "CODEA",
                    DiscountType = "Percentage",
                    DiscountValue = 10m,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(5),
                    IsActive = true
                },
                new Promotion
                {
                    PromotionID = 2,
                    Title = "Promo B",
                    PromoCode = "CODEB",
                    DiscountType = "Percentage",
                    DiscountValue = 15m,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(5),
                    IsActive = true
                });
            await dbContext.SaveChangesAsync();

            var request = new UpdatePromoRequest
            {
                Title = "Promo B",
                PromoCode = "codea",
                DiscountType = "Percentage",
                DiscountValue = 15m,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(5),
                IsActive = true
            };

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => service.UpdatePromotionAsync(2, request));
            Assert.Equal("PROMO_CODE_EXISTS", exception.Message);
        }

        [Fact]
        public async Task GetPromoUsageAsync_ExcludesBookingsOlderThan365Days()
        {
            var dbContext = CreateDbContext();
            var service = new PromotionService(dbContext);

            var promo = new Promotion
            {
                PromotionID = 1,
                Title = "Old Promo",
                PromoCode = "OLD365",
                DiscountType = "Fixed_Amount",
                DiscountValue = 20000m,
                StartDate = DateTime.UtcNow.AddDays(-800),
                EndDate = DateTime.UtcNow.AddDays(30),
                IsActive = true
            };

            var oldBooking = new Booking
            {
                BookingID = 1,
                CustomerID = 1,
                PromotionID = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-400), // older than 365 days -> excluded
                DiscountApplied = 10000m,
                FinalAmount = 100000m,
                Status = BookingStatus.Completed
            };

            var recentBooking = new Booking
            {
                BookingID = 2,
                CustomerID = 2,
                PromotionID = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-10), // within 365 days -> included
                DiscountApplied = 15000m,
                FinalAmount = 150000m,
                Status = BookingStatus.Completed
            };

            dbContext.Promotions.Add(promo);
            dbContext.Bookings.AddRange(oldBooking, recentBooking);
            await dbContext.SaveChangesAsync();

            var result = await service.GetPromoUsageAsync(1);

            Assert.Equal(1, result.TotalUsageCount);
            Assert.Equal(15000m, result.TotalDiscountAmount);
            Assert.Equal(150000m, result.TotalRevenueGenerated);
            Assert.Single(result.Usages);
            Assert.Equal(2, result.Usages[0].BookingId);
        }

        [Fact]
        public async Task GetPromoUsageAsync_ClampsRangeStartToPromoStartDateWhenYounger()
        {
            var dbContext = CreateDbContext();
            var service = new PromotionService(dbContext);

            var promoStart = DateTime.UtcNow.AddDays(-30).Date;
            var promo = new Promotion
            {
                PromotionID = 1,
                Title = "New Promo",
                PromoCode = "NEW30",
                DiscountType = "Fixed_Amount",
                DiscountValue = 10000m,
                StartDate = promoStart,
                EndDate = DateTime.UtcNow.AddDays(30),
                IsActive = true
            };

            dbContext.Promotions.Add(promo);
            await dbContext.SaveChangesAsync();

            var result = await service.GetPromoUsageAsync(1);

            Assert.Equal(promoStart.ToString("yyyy-MM-dd"), result.RangeStart);
        }

        [Fact]
        public async Task GetPromoUsageAsync_ClampsRangeStartTo365DaysWhenPromoOlder()
        {
            var dbContext = CreateDbContext();
            var service = new PromotionService(dbContext);

            var promo = new Promotion
            {
                PromotionID = 1,
                Title = "Long Running Promo",
                PromoCode = "LONGRUN",
                DiscountType = "Fixed_Amount",
                DiscountValue = 10000m,
                StartDate = DateTime.UtcNow.AddDays(-800).Date,
                EndDate = DateTime.UtcNow.AddDays(30),
                IsActive = true
            };

            dbContext.Promotions.Add(promo);
            await dbContext.SaveChangesAsync();

            var expectedStart = DateTime.UtcNow.AddHours(7).Date.AddDays(-365);

            var result = await service.GetPromoUsageAsync(1);

            Assert.Equal(expectedStart.ToString("yyyy-MM-dd"), result.RangeStart);
        }
    }
}
