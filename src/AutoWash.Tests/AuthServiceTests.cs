using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using AutoWash.Application.DTOs;
using AutoWash.Application.Services;
using AutoWash.Domain.Entities;
using AutoWash.Infrastructure.Data;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace AutoWash.Tests.Application.Services
{
  public class AuthServiceTests
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
    public async Task RegisterAsync_WithValidData_ShouldCreateCustomerAndLoyaltyAccount()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var config = CreateConfiguration();
      var authService = new AuthService(dbContext, config);

      var registerRequest = new RegisterRequest
      {
        FullName = "Nguyễn Văn Test",
        Phone = "0901234567",
        Password = "Password123",
        ConfirmPassword = "Password123"
      };

      // Act
      var result = await authService.RegisterAsync(registerRequest);

      // Assert
      Assert.NotNull(result);
      Assert.Equal("Nguyễn Văn Test", result.FullName);
      Assert.Equal("0901234567", result.Phone);
      Assert.Equal("Member", result.Tier);

      // Verify customer was created
      var customer = await dbContext.Customers.FirstOrDefaultAsync(c => c.Phone == "0901234567");
      Assert.NotNull(customer);
      Assert.Equal("Nguyễn Văn Test", customer.FullName);
      Assert.False(customer.IsLocked);

      // Verify loyalty account was created
      var loyaltyAccount = await dbContext.LoyaltyAccounts.FirstOrDefaultAsync(l => l.CustomerID == customer.CustomerID);
      Assert.NotNull(loyaltyAccount);
      Assert.Equal(0, loyaltyAccount.TotalPoints);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicatePhone_ShouldThrowInvalidOperationException()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var config = CreateConfiguration();
      var authService = new AuthService(dbContext, config);

      // Create existing customer
      var existingCustomer = new Customer
      {
        FullName = "Existing User",
        Phone = "0901234567",
        Password = BCrypt.Net.BCrypt.HashPassword("Password123"),
        Tier = "Member",
        IsLocked = false,
        CreatedAt = DateTime.UtcNow
      };
      dbContext.Customers.Add(existingCustomer);
      await dbContext.SaveChangesAsync();

      var registerRequest = new RegisterRequest
      {
        FullName = "Nguyễn Văn Test",
        Phone = "0901234567",
        Password = "Password123",
        ConfirmPassword = "Password123"
      };

      // Act & Assert
      var exception = await Assert.ThrowsAsync<InvalidOperationException>(
          () => authService.RegisterAsync(registerRequest));
      Assert.Equal("PHONE_ALREADY_EXISTS", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnAuthResponse()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var config = CreateConfiguration();
      var authService = new AuthService(dbContext, config);

      var password = "Password123";
      var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

      var customer = new Customer
      {
        FullName = "Test User",
        Phone = "0901234567",
        Password = hashedPassword,
        Tier = "1",
        IsLocked = false,
        CreatedAt = DateTime.UtcNow
      };

      dbContext.Customers.Add(customer);
      await dbContext.SaveChangesAsync();

      var loginRequest = new LoginRequest
      {
        Phone = "0901234567",
        Password = password
      };

      // Act
      var result = await authService.LoginAsync(loginRequest);

      // Assert
      Assert.NotNull(result);
      Assert.Equal("Test User", result.FullName);
      Assert.Equal("0901234567", result.Phone);
      Assert.Equal("Member", result.Tier);
      Assert.NotEmpty(result.Token);
      Assert.False(result.IsLocked);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldThrowUnauthorizedAccessException()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var config = CreateConfiguration();
      var authService = new AuthService(dbContext, config);

      var hashedPassword = BCrypt.Net.BCrypt.HashPassword("CorrectPassword");

      var customer = new Customer
      {
        FullName = "Test User",
        Phone = "0901234567",
        Password = hashedPassword,
        Tier = "1",
        IsLocked = false,
        CreatedAt = DateTime.UtcNow
      };

      dbContext.Customers.Add(customer);
      await dbContext.SaveChangesAsync();

      var loginRequest = new LoginRequest
      {
        Phone = "0901234567",
        Password = "WrongPassword"
      };

      // Act & Assert
      var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
          () => authService.LoginAsync(loginRequest));
      Assert.Equal("INVALID_CREDENTIALS", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_WithLockedAccount_ShouldThrowInvalidOperationException()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var config = CreateConfiguration();
      var authService = new AuthService(dbContext, config);

      var password = "Password123";
      var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

      var customer = new Customer
      {
        FullName = "Test User",
        Phone = "0901234567",
        Password = hashedPassword,
        Tier = "1",
        IsLocked = true, // Account is locked
        CreatedAt = DateTime.UtcNow
      };

      dbContext.Customers.Add(customer);
      await dbContext.SaveChangesAsync();

      var loginRequest = new LoginRequest
      {
        Phone = "0901234567",
        Password = password
      };

      // Act & Assert
      var exception = await Assert.ThrowsAsync<InvalidOperationException>(
          () => authService.LoginAsync(loginRequest));
      Assert.Equal("ACCOUNT_LOCKED", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentPhone_ShouldThrowUnauthorizedAccessException()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var config = CreateConfiguration();
      var authService = new AuthService(dbContext, config);

      var loginRequest = new LoginRequest
      {
        Phone = "0999999999",
        Password = "AnyPassword"
      };

      // Act & Assert
      var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
          () => authService.LoginAsync(loginRequest));
      Assert.Equal("INVALID_CREDENTIALS", exception.Message);
    }

    [Fact]
    public async Task GetCustomerIdFromToken_WithValidToken_ShouldReturnCustomerId()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var config = CreateConfiguration();
      var authService = new AuthService(dbContext, config);

      var password = "Password123";
      var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

      var customer = new Customer
      {
        FullName = "Test User",
        Phone = "0901234567",
        Password = hashedPassword,
        Tier = "1",
        IsLocked = false,
        CreatedAt = DateTime.UtcNow
      };

      dbContext.Customers.Add(customer);
      await dbContext.SaveChangesAsync();

      var loginRequest = new LoginRequest
      {
        Phone = "0901234567",
        Password = password
      };

      var authResponse = await authService.LoginAsync(loginRequest);

      // Act
      var customerId = authService.GetCustomerIdFromToken(authResponse.Token);

      // Assert
      Assert.NotNull(customerId);
      Assert.Equal(customer.CustomerID, customerId);
    }

    [Fact]
    public void GetCustomerIdFromToken_WithInvalidToken_ShouldReturnNull()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var config = CreateConfiguration();
      var authService = new AuthService(dbContext, config);

      // Act
      var customerId = authService.GetCustomerIdFromToken("invalid.token.here");

      // Assert
      Assert.Null(customerId);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ShouldReturnTrue()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var config = CreateConfiguration();
      var authService = new AuthService(dbContext, config);

      var password = "Password123";
      var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

      var customer = new Customer
      {
        FullName = "Test User",
        Phone = "0901234567",
        Password = hashedPassword,
        Tier = "1",
        IsLocked = false,
        CreatedAt = DateTime.UtcNow
      };

      dbContext.Customers.Add(customer);
      await dbContext.SaveChangesAsync();

      var loginRequest = new LoginRequest
      {
        Phone = "0901234567",
        Password = password
      };

      var authResponse = await authService.LoginAsync(loginRequest);

      // Act
      var isValid = await authService.ValidateTokenAsync(authResponse.Token);

      // Assert
      Assert.True(isValid);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithInvalidToken_ShouldReturnFalse()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var config = CreateConfiguration();
      var authService = new AuthService(dbContext, config);

      // Act
      var isValid = await authService.ValidateTokenAsync("invalid.token.here");

      // Assert
      Assert.False(isValid);
    }
  }
}
