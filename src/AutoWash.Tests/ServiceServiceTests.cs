using AutoWash.Application.DTOs;
using AutoWash.Application.Services;
using AutoWash.Domain.Entities;
using AutoWash.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AutoWash.Tests.Application.Services
{
  public class ServiceServiceTests
  {
    private static ApplicationDbContext CreateDbContext()
    {
      var options = new DbContextOptionsBuilder<ApplicationDbContext>()
          .UseInMemoryDatabase(Guid.NewGuid().ToString())
          .Options;

      return new ApplicationDbContext(options);
    }

    private static ServiceService CreateService(ApplicationDbContext dbContext) =>
        new ServiceService(dbContext, Mock.Of<ILogger<ServiceService>>());

    [Fact]
    public async Task GetActiveServicesAsync_ShouldOnlyReturnActiveServices()
    {
      using var dbContext = CreateDbContext();
      dbContext.Services.Add(new Service { ServiceName = "Rửa cơ bản", ServiceCategory = "Basic", Description = "d", Price = 50000m, Duration = 30, Status = "Active" });
      dbContext.Services.Add(new Service { ServiceName = "Rửa VIP", ServiceCategory = "VIP", Description = "d", Price = 150000m, Duration = 60, Status = "Inactive" });
      await dbContext.SaveChangesAsync();

      var service = CreateService(dbContext);
      var services = (await service.GetActiveServicesAsync()).ToList();

      Assert.Single(services);
      Assert.Equal("Rửa cơ bản", services[0].ServiceName);
    }

    [Fact]
    public async Task GetServiceByIdAsync_WithExistingService_ShouldReturnIt()
    {
      using var dbContext = CreateDbContext();
      var entity = new Service { ServiceName = "Rửa cơ bản", ServiceCategory = "Basic", Description = "d", Price = 50000m, Duration = 30, Status = "Active" };
      dbContext.Services.Add(entity);
      await dbContext.SaveChangesAsync();

      var service = CreateService(dbContext);
      var result = await service.GetServiceByIdAsync(entity.ServiceID);

      Assert.Equal(entity.ServiceID, result.ServiceId);
      Assert.Equal("Rửa cơ bản", result.ServiceName);
    }

    [Fact]
    public async Task GetServiceByIdAsync_WithNonExistentService_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var service = CreateService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.GetServiceByIdAsync(999));

      Assert.StartsWith("NOT_FOUND", ex.Message);
    }

    [Fact]
    public async Task CreateServiceAsync_WithValidData_ShouldPersistAsActive()
    {
      using var dbContext = CreateDbContext();
      var service = CreateService(dbContext);

      var result = await service.CreateServiceAsync(new CreateServiceRequest
      {
        ServiceName = "Rửa nội thất",
        ServiceCategory = "Interior",
        Description = "Vệ sinh nội thất",
        Price = 200000m,
        Duration = 45
      });

      Assert.Equal("Active", result.Status);
      Assert.Equal(1, await dbContext.Services.CountAsync());
    }

    [Theory]
    [InlineData("", "cat", "desc", 10000, 30)]
    [InlineData("name", "", "desc", 10000, 30)]
    [InlineData("name", "cat", "", 10000, 30)]
    [InlineData("name", "cat", "desc", 0, 30)]
    [InlineData("name", "cat", "desc", 10000, 0)]
    public async Task CreateServiceAsync_WithInvalidData_ShouldThrow(string name, string category, string description, decimal price, int duration)
    {
      using var dbContext = CreateDbContext();
      var service = CreateService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.CreateServiceAsync(new CreateServiceRequest
      {
        ServiceName = name,
        ServiceCategory = category,
        Description = description,
        Price = price,
        Duration = duration
      }));

      Assert.StartsWith("INVALID_REQUEST", ex.Message);
    }

    [Fact]
    public async Task UpdateServiceAsync_WithPartialFields_ShouldOnlyChangeProvidedFields()
    {
      using var dbContext = CreateDbContext();
      var entity = new Service { ServiceName = "Old", ServiceCategory = "Basic", Description = "Old desc", Price = 50000m, Duration = 30, Status = "Active" };
      dbContext.Services.Add(entity);
      await dbContext.SaveChangesAsync();

      var service = CreateService(dbContext);
      var result = await service.UpdateServiceAsync(entity.ServiceID, new UpdateServiceRequest
      {
        Price = 75000m
      });

      Assert.Equal(75000m, result.Price);
      Assert.Equal("Old", result.ServiceName);
      Assert.Equal(30, result.Duration);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1000)]
    public async Task UpdateServiceAsync_WithNonPositivePrice_ShouldThrow(int price)
    {
      using var dbContext = CreateDbContext();
      var entity = new Service { ServiceName = "Old", ServiceCategory = "Basic", Description = "Old desc", Price = 50000m, Duration = 30, Status = "Active" };
      dbContext.Services.Add(entity);
      await dbContext.SaveChangesAsync();

      var service = CreateService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.UpdateServiceAsync(entity.ServiceID, new UpdateServiceRequest
      {
        Price = price
      }));

      Assert.StartsWith("INVALID_REQUEST", ex.Message);
      Assert.Equal(50000m, entity.Price); // không bị thay đổi khi request không hợp lệ
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task UpdateServiceAsync_WithNonPositiveDuration_ShouldThrow(int duration)
    {
      using var dbContext = CreateDbContext();
      var entity = new Service { ServiceName = "Old", ServiceCategory = "Basic", Description = "Old desc", Price = 50000m, Duration = 30, Status = "Active" };
      dbContext.Services.Add(entity);
      await dbContext.SaveChangesAsync();

      var service = CreateService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.UpdateServiceAsync(entity.ServiceID, new UpdateServiceRequest
      {
        Duration = duration
      }));

      Assert.StartsWith("INVALID_REQUEST", ex.Message);
      Assert.Equal(30, entity.Duration); // không bị thay đổi khi request không hợp lệ
    }

    [Fact]
    public async Task UpdateServiceAsync_WithNonExistentService_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var service = CreateService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.UpdateServiceAsync(999, new UpdateServiceRequest { Price = 1000m }));

      Assert.StartsWith("NOT_FOUND", ex.Message);
    }

    [Fact]
    public async Task ToggleServiceStatusAsync_ShouldFlipStatus()
    {
      using var dbContext = CreateDbContext();
      var entity = new Service { ServiceName = "Svc", ServiceCategory = "Basic", Description = "d", Price = 50000m, Duration = 30, Status = "Active" };
      dbContext.Services.Add(entity);
      await dbContext.SaveChangesAsync();

      var service = CreateService(dbContext);

      var afterFirst = await service.ToggleServiceStatusAsync(entity.ServiceID);
      Assert.Equal("Inactive", afterFirst.Status);

      var afterSecond = await service.ToggleServiceStatusAsync(entity.ServiceID);
      Assert.Equal("Active", afterSecond.Status);
    }

    [Fact]
    public async Task ToggleServiceStatusAsync_WithNonExistentService_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var service = CreateService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.ToggleServiceStatusAsync(999));

      Assert.StartsWith("NOT_FOUND", ex.Message);
    }
  }
}
