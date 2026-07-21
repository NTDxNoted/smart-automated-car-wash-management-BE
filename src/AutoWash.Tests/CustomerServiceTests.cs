using AutoWash.Application.DTOs;
using AutoWash.Application.Services;
using AutoWash.Domain.Entities;
using AutoWash.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AutoWash.Tests.Application.Services
{
  public class CustomerServiceTests
  {
    private static ApplicationDbContext CreateDbContext()
    {
      var options = new DbContextOptionsBuilder<ApplicationDbContext>()
          .UseInMemoryDatabase(Guid.NewGuid().ToString())
          .Options;

      return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetProfileAsync_WithExistingCustomer_ShouldReturnProfileAndLoyaltyPoints()
    {
      using var dbContext = CreateDbContext();
      var customer = new Customer
      {
        FullName = "Test User",
        Phone = "0901234567",
        Password = "hashed",
        TotalSpending = 500000m,
        CreatedAt = DateTime.UtcNow
      };
      dbContext.Customers.Add(customer);
      await dbContext.SaveChangesAsync();

      dbContext.LoyaltyAccounts.Add(new LoyaltyAccount
      {
        CustomerID = customer.CustomerID,
        TotalPoints = 42,
        LastUpdated = DateTime.UtcNow
      });
      await dbContext.SaveChangesAsync();

      var service = new CustomerService(dbContext);

      var profile = await service.GetProfileAsync(customer.CustomerID);

      Assert.Equal(customer.CustomerID, profile.CustomerId);
      Assert.Equal("Test User", profile.FullName);
      Assert.Equal("0901234567", profile.Phone);
      Assert.Equal(500000m, profile.TotalSpending);
      Assert.Equal(42, profile.LoyaltyPoints);
    }

    [Fact]
    public async Task GetProfileAsync_WithoutLoyaltyAccount_ShouldReturnZeroPoints()
    {
      using var dbContext = CreateDbContext();
      var customer = new Customer
      {
        FullName = "No Loyalty",
        Phone = "0909999999",
        Password = "hashed",
        CreatedAt = DateTime.UtcNow
      };
      dbContext.Customers.Add(customer);
      await dbContext.SaveChangesAsync();

      var service = new CustomerService(dbContext);

      var profile = await service.GetProfileAsync(customer.CustomerID);

      Assert.Equal(0, profile.LoyaltyPoints);
    }

    [Fact]
    public async Task GetProfileAsync_WithNonExistentCustomer_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var service = new CustomerService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.GetProfileAsync(999));

      Assert.StartsWith("NOT_FOUND", ex.Message);
    }

    [Fact]
    public async Task UpdateProfileAsync_WithNewFullName_ShouldPersistChange()
    {
      using var dbContext = CreateDbContext();
      var customer = new Customer
      {
        FullName = "Old Name",
        Phone = "0901111111",
        Password = "hashed",
        CreatedAt = DateTime.UtcNow
      };
      dbContext.Customers.Add(customer);
      await dbContext.SaveChangesAsync();

      var service = new CustomerService(dbContext);

      var updated = await service.UpdateProfileAsync(customer.CustomerID, new UpdateProfileRequest
      {
        FullName = "New Name"
      });

      Assert.Equal("New Name", updated.FullName);

      var persisted = await dbContext.Customers.FirstAsync(c => c.CustomerID == customer.CustomerID);
      Assert.Equal("New Name", persisted.FullName);
    }

    [Fact]
    public async Task UpdateProfileAsync_WithBlankFullName_ShouldKeepExistingName()
    {
      using var dbContext = CreateDbContext();
      var customer = new Customer
      {
        FullName = "Keep Me",
        Phone = "0902222222",
        Password = "hashed",
        CreatedAt = DateTime.UtcNow
      };
      dbContext.Customers.Add(customer);
      await dbContext.SaveChangesAsync();

      var service = new CustomerService(dbContext);

      var updated = await service.UpdateProfileAsync(customer.CustomerID, new UpdateProfileRequest
      {
        FullName = "   "
      });

      Assert.Equal("Keep Me", updated.FullName);
    }

    [Fact]
    public async Task UpdateProfileAsync_WithNonExistentCustomer_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var service = new CustomerService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.UpdateProfileAsync(999, new UpdateProfileRequest { FullName = "X" }));

      Assert.StartsWith("NOT_FOUND", ex.Message);
    }
  }
}
