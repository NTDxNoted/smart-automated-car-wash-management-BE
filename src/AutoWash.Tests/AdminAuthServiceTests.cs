using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using AutoWash.Application.DTOs;
using AutoWash.Application.Services;
using AutoWash.Domain.Entities;
using AutoWash.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoWash.Tests.Application.Services
{
    public class AdminAuthServiceTests
    {
        private ApplicationDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private IConfiguration CreateConfiguration()
        {
            var inMemorySettings = new Dictionary<string, string> {
                {"Jwt:SecretKey", "AutoWashPro_SecretKey_2025_MustBe32CharsLong!!"},
                {"Jwt:Issuer", "AutoWashAPI"},
                {"Jwt:Audience", "AutoWashClient"},
                {"Jwt:ExpiryMinutes", "1440"}
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        [Fact]
        public async Task LoginAsync_WithValidAdminCredentials_ShouldReturnAdminLoginResponse()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var config = CreateConfiguration();
            var adminAuthService = new AdminAuthService(dbContext, config);

            var password = "password123";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            var admin = new Customer
            {
                FullName = "Super Admin",
                Phone = "0999999991",
                Password = hashedPassword,
                Role = "ADMIN",
                IsLocked = false,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Customers.Add(admin);
            await dbContext.SaveChangesAsync();

            var loginRequest = new AdminLoginRequest
            {
                Phone = "0999999991",
                Password = password
            };

            // Act
            var result = await adminAuthService.LoginAsync(loginRequest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Super Admin", result.FullName);
            Assert.Equal("Admin", result.Role);
            Assert.NotEmpty(result.Token);
            Assert.Equal(admin.CustomerID, result.AdminId);
        }

        [Fact]
        public async Task LoginAsync_WithMemberCredentials_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var config = CreateConfiguration();
            var adminAuthService = new AdminAuthService(dbContext, config);

            var password = "password123";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            var member = new Customer
            {
                FullName = "Normal Member",
                Phone = "0901111001",
                Password = hashedPassword,
                Role = "MEMBER",
                IsLocked = false,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Customers.Add(member);
            await dbContext.SaveChangesAsync();

            var loginRequest = new AdminLoginRequest
            {
                Phone = "0901111001",
                Password = password
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => adminAuthService.LoginAsync(loginRequest));
            Assert.Equal("INVALID_CREDENTIALS", exception.Message);
        }

        [Fact]
        public async Task LoginAsync_WithLockedAdmin_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var config = CreateConfiguration();
            var adminAuthService = new AdminAuthService(dbContext, config);

            var password = "password123";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            var admin = new Customer
            {
                FullName = "Locked Admin",
                Phone = "0999999991",
                Password = hashedPassword,
                Role = "ADMIN",
                IsLocked = true,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Customers.Add(admin);
            await dbContext.SaveChangesAsync();

            var loginRequest = new AdminLoginRequest
            {
                Phone = "0999999991",
                Password = password
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => adminAuthService.LoginAsync(loginRequest));
            Assert.Equal("ACCOUNT_LOCKED", exception.Message);
        }

        [Fact]
        public async Task GetProfileAsync_WithValidAdminId_ShouldReturnAdminProfileResponse()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var config = CreateConfiguration();
            var adminAuthService = new AdminAuthService(dbContext, config);

            var admin = new Customer
            {
                FullName = "Super Admin",
                Phone = "0999999991",
                Password = "hashedpassword",
                Role = "ADMIN",
                IsLocked = false,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Customers.Add(admin);
            await dbContext.SaveChangesAsync();

            // Act
            var result = await adminAuthService.GetProfileAsync(admin.CustomerID);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Super Admin", result.FullName);
            Assert.Equal("0999999991", result.Phone);
            Assert.Equal("Admin", result.Role);
            Assert.Equal(admin.CustomerID, result.AdminId);
        }

        [Fact]
        public async Task GetProfileAsync_WithMemberId_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var config = CreateConfiguration();
            var adminAuthService = new AdminAuthService(dbContext, config);

            var member = new Customer
            {
                FullName = "Normal Member",
                Phone = "0901111001",
                Password = "hashedpassword",
                Role = "MEMBER",
                IsLocked = false,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Customers.Add(member);
            await dbContext.SaveChangesAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => adminAuthService.GetProfileAsync(member.CustomerID));
            Assert.Equal("ADMIN_NOT_FOUND", exception.Message);
        }
    }
}
