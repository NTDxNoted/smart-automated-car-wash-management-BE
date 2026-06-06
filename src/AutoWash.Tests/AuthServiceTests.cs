using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoWash.Application.DTOs;
using AutoWash.Application.Exceptions;
using AutoWash.Application.Services;
using AutoWash.Infrastructure.Data;
using AutoWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AutoWash.Tests
{
  public class AuthServiceTests
  {
    private static ApplicationDbContext CreateContext()
    {
      var options = new DbContextOptionsBuilder<ApplicationDbContext>()
          .UseInMemoryDatabase(Guid.NewGuid().ToString())
          .Options;

      return new ApplicationDbContext(options);
    }

    private static IConfiguration CreateConfiguration()
    {
      return new ConfigurationBuilder()
          .AddInMemoryCollection(new Dictionary<string, string?>
          {
            ["Jwt:Secret"] = "TestJwtSecretKeyForUnitTests12345"
          })
          .Build();
    }

    [Fact]
    public async Task Register_ShouldCreateCustomer_WhenPhoneIsUnique()
    {
      using var context = CreateContext();
      var service = new AuthService(context, CreateConfiguration(), NullLogger<AuthService>.Instance);

      var request = new RegisterRequest
      {
        FullName = "Nguyễn Văn X",
        Phone = "0901111011",
        Password = "SecureP@ssw0rd"
      };

      var result = await service.RegisterAsync(request);

      Assert.NotEqual(0, result.CustomerId);
      Assert.Equal("Nguyễn Văn X", result.FullName);
      Assert.Equal("0901111011", result.Phone);
      Assert.Equal("Member", result.Tier);
      Assert.False(result.IsLocked);
      Assert.NotNull(result.CreatedAt);
    }

    [Fact]
    public async Task Register_ShouldThrow_WhenPhoneAlreadyExists()
    {
      using var context = CreateContext();
      context.Customers.Add(new Customer
      {
        FullName = "Existing User",
        Phone = "0909999000",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123")
      });
      await context.SaveChangesAsync();

      var service = new AuthService(context, CreateConfiguration(), NullLogger<AuthService>.Instance);

      var request = new RegisterRequest
      {
        FullName = "Another User",
        Phone = "0909999000",
        Password = "SecureP@ssw0rd"
      };

      var ex = await Assert.ThrowsAsync<AuthException>(() => service.RegisterAsync(request));
      Assert.Equal("PHONE_ALREADY_EXISTS", ex.ErrorCode);
    }

    [Fact]
    public async Task Login_ShouldThrow_WhenPasswordIsInvalid()
    {
      using var context = CreateContext();
      context.Customers.Add(new Customer
      {
        FullName = "Member User",
        Phone = "0902222333",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword")
      });
      await context.SaveChangesAsync();

      var service = new AuthService(context, CreateConfiguration(), NullLogger<AuthService>.Instance);

      var request = new LoginRequest
      {
        Phone = "0902222333",
        Password = "WrongPassword"
      };

      var ex = await Assert.ThrowsAsync<AuthException>(() => service.LoginAsync(request));
      Assert.Equal("INVALID_CREDENTIALS", ex.ErrorCode);
    }

    [Fact]
    public async Task Login_ShouldThrow_WhenAccountIsLocked()
    {
      using var context = CreateContext();
      context.Customers.Add(new Customer
      {
        FullName = "Locked User",
        Phone = "0903333444",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("LockedPassword"),
        IsLocked = true
      });
      await context.SaveChangesAsync();

      var service = new AuthService(context, CreateConfiguration(), NullLogger<AuthService>.Instance);

      var request = new LoginRequest
      {
        Phone = "0903333444",
        Password = "LockedPassword"
      };

      var ex = await Assert.ThrowsAsync<AuthException>(() => service.LoginAsync(request));
      Assert.Equal("ACCOUNT_LOCKED", ex.ErrorCode);
    }
  }
}
