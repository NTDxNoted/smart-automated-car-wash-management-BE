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
        public async Task GetPromotionsAsync_ShouldReturnAllPromotionsNewestFirst()
        {
            var dbContext = CreateDbContext();
            dbContext.Promotions.Add(new Promotion { Title = "Old", PromoCode = "OLD", DiscountType = "Fixed_Amount", DiscountValue = 10000m, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(5) });
            await dbContext.SaveChangesAsync();
            dbContext.Promotions.Add(new Promotion { Title = "New", PromoCode = "NEW", DiscountType = "Fixed_Amount", DiscountValue = 10000m, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(5) });
            await dbContext.SaveChangesAsync();

            var service = new PromotionService(dbContext);
            var result = (await service.GetPromotionsAsync()).ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal("New", result[0].Title);
        }

        [Fact]
        public async Task UpdatePromotionAsync_WithExistingPromo_ShouldPersistChanges()
        {
            var dbContext = CreateDbContext();
            var promo = new Promotion { Title = "Old Title", PromoCode = "CODE1", DiscountType = "Fixed_Amount", DiscountValue = 10000m, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(5), IsActive = true, MinOrderValue = 0 };
            dbContext.Promotions.Add(promo);
            await dbContext.SaveChangesAsync();

            var service = new PromotionService(dbContext);
            var result = await service.UpdatePromotionAsync(promo.PromotionID, new UpdatePromoRequest
            {
                Title = "New Title",
                DiscountType = "Percentage",
                DiscountValue = 20m,
                StartDate = promo.StartDate,
                EndDate = promo.EndDate,
                IsActive = true,
                MinOrderValue = 0
            });

            Assert.Equal("New Title", result.Title);
            Assert.Equal("Percentage", result.DiscountType);
            Assert.Equal(20m, result.DiscountValue);
        }

        [Fact]
        public async Task UpdatePromotionAsync_WithNonExistentPromo_ShouldThrow()
        {
            var dbContext = CreateDbContext();
            var service = new PromotionService(dbContext);

            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdatePromotionAsync(999, new UpdatePromoRequest
            {
                Title = "X",
                DiscountType = "Fixed_Amount",
                DiscountValue = 1000m,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1),
                MinOrderValue = 0
            }));

            Assert.Equal("PROMO_NOT_FOUND", ex.Message);
        }

        [Fact]
        public async Task TogglePromoActiveAsync_ShouldFlipIsActive()
        {
            var dbContext = CreateDbContext();
            var promo = new Promotion { Title = "Promo", PromoCode = "CODE2", DiscountType = "Fixed_Amount", DiscountValue = 10000m, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(5), IsActive = true };
            dbContext.Promotions.Add(promo);
            await dbContext.SaveChangesAsync();

            var service = new PromotionService(dbContext);

            var afterFirst = await service.TogglePromoActiveAsync(promo.PromotionID);
            Assert.False(afterFirst.IsActive);

            var afterSecond = await service.TogglePromoActiveAsync(promo.PromotionID);
            Assert.True(afterSecond.IsActive);
        }

        [Fact]
        public async Task TogglePromoActiveAsync_WithNonExistentPromo_ShouldThrow()
        {
            var dbContext = CreateDbContext();
            var service = new PromotionService(dbContext);

            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => service.TogglePromoActiveAsync(999));

            Assert.Equal("PROMO_NOT_FOUND", ex.Message);
        }

        [Fact]
        public async Task GetPromoUsageAsync_ShouldReturnBookingsThatUsedThePromo()
        {
            var dbContext = CreateDbContext();
            var promo = new Promotion { Title = "Promo", PromoCode = "CODE3", DiscountType = "Fixed_Amount", DiscountValue = 10000m, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(5), IsActive = true };
            dbContext.Promotions.Add(promo);
            await dbContext.SaveChangesAsync();

            dbContext.Customers.Add(new Customer { CustomerID = 1, FullName = "Test Customer", Phone = "0901234567", Password = "pw", CreatedAt = DateTime.UtcNow });
            dbContext.Bookings.Add(new Booking
            {
                CustomerID = 1,
                Phone = "0901234567",
                VehicleID = 1,
                LicensePlate = "51A-123.45",
                ServiceID = 1,
                PromotionID = promo.PromotionID,
                ScheduledTime = DateTime.UtcNow,
                Status = BookingStatus.Completed,
                DiscountApplied = 10000m,
                CreatedAt = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync();

            var service = new PromotionService(dbContext);
            var result = (await service.GetPromoUsageAsync(promo.PromotionID)).ToList();

            Assert.Single(result);
            Assert.Equal("Test Customer", result[0].CustomerName);
            Assert.Equal(10000m, result[0].DiscountAmountActual);
        }

        [Fact]
        public async Task GetPromoUsageAsync_WithNonExistentPromo_ShouldThrow()
        {
            var dbContext = CreateDbContext();
            var service = new PromotionService(dbContext);

            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetPromoUsageAsync(999));

            Assert.Equal("PROMO_NOT_FOUND", ex.Message);
        }
    }
}
