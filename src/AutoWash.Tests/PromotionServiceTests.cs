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
    }
}
