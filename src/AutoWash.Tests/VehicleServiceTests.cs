using AutoWash.Application.DTOs;
using AutoWash.Application.Services;
using AutoWash.Domain.Entities;
using AutoWash.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AutoWash.Tests.Application.Services
{
  public class VehicleServiceTests
  {
    private static ApplicationDbContext CreateDbContext()
    {
      var options = new DbContextOptionsBuilder<ApplicationDbContext>()
          .UseInMemoryDatabase(Guid.NewGuid().ToString())
          .Options;

      return new ApplicationDbContext(options);
    }

    private static (VehicleService service, Customer customer) CreateService(ApplicationDbContext dbContext)
    {
      var customer = new Customer
      {
        FullName = "Test User",
        Phone = "0901234567",
        Password = "hashed",
        Role = "MEMBER",
        IsLocked = false,
        CreatedAt = DateTime.UtcNow
      };
      dbContext.Customers.Add(customer);
      dbContext.SaveChanges();

      var service = new VehicleService(dbContext);

      return (service, customer);
    }

    [Fact]
    public async Task AddVehicleAsync_WithValidData_ShouldCreateVehicle()
    {
      using var dbContext = CreateDbContext();
      var (service, customer) = CreateService(dbContext);

      var result = await service.AddVehicleAsync(customer.CustomerID, new AddVehicleRequest
      {
        LicensePlate = "51A-123.45"
      });

      Assert.NotNull(result);
      Assert.Equal("51A-123.45", result.LicensePlate);
      Assert.True(result.IsActive);
    }

    [Fact]
    public async Task AddVehicleAsync_WithDuplicatePlate_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var (service, customer) = CreateService(dbContext);

      await service.AddVehicleAsync(customer.CustomerID, new AddVehicleRequest { LicensePlate = "51A-123.45" });

      var ex = await Assert.ThrowsAsync<Exception>(() => service.AddVehicleAsync(customer.CustomerID, new AddVehicleRequest
      {
        LicensePlate = "51A-123.45"
      }));

      Assert.StartsWith("PLATE_ALREADY_SAVED", ex.Message);
    }

    [Fact]
    public async Task AddVehicleAsync_WithLowercaseOrPaddedPlate_ShouldNormalizeToUppercaseTrimmed()
    {
      using var dbContext = CreateDbContext();
      var (service, customer) = CreateService(dbContext);

      var result = await service.AddVehicleAsync(customer.CustomerID, new AddVehicleRequest
      {
        LicensePlate = " 51f-123.45 "
      });

      Assert.Equal("51F-123.45", result.LicensePlate);
    }

    [Fact]
    public async Task AddVehicleAsync_WithDuplicatePlateDifferingByCase_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var (service, customer) = CreateService(dbContext);

      await service.AddVehicleAsync(customer.CustomerID, new AddVehicleRequest { LicensePlate = "51A-123.45" });

      var ex = await Assert.ThrowsAsync<Exception>(() => service.AddVehicleAsync(customer.CustomerID, new AddVehicleRequest
      {
        LicensePlate = "51a-123.45"
      }));

      Assert.StartsWith("PLATE_ALREADY_SAVED", ex.Message);
    }

    [Fact]
    public async Task UpdateVehicleAsync_WithValidData_ShouldUpdatePlate()
    {
      using var dbContext = CreateDbContext();
      var (service, customer) = CreateService(dbContext);

      var vehicle = await service.AddVehicleAsync(customer.CustomerID, new AddVehicleRequest { LicensePlate = "51A-123.45" });

      var result = await service.UpdateVehicleAsync(customer.CustomerID, vehicle.VehicleId, new UpdateVehicleRequest
      {
        LicensePlate = "51A-999.99"
      });

      Assert.Equal("51A-999.99", result.LicensePlate);
    }

    [Fact]
    public async Task DeleteVehicleAsync_ShouldSoftDeleteVehicle()
    {
      using var dbContext = CreateDbContext();
      var (service, customer) = CreateService(dbContext);

      var vehicle = await service.AddVehicleAsync(customer.CustomerID, new AddVehicleRequest { LicensePlate = "51A-123.45" });

      await service.DeleteVehicleAsync(customer.CustomerID, vehicle.VehicleId);

      var vehicles = await service.GetVehiclesAsync(customer.CustomerID);
      Assert.Empty(vehicles);
    }

    [Fact]
    public async Task DeleteVehicleAsync_WithVehicleNotOwnedByCustomer_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var (service, customer) = CreateService(dbContext);

      var vehicle = await service.AddVehicleAsync(customer.CustomerID, new AddVehicleRequest { LicensePlate = "51A-123.45" });

      var ex = await Assert.ThrowsAsync<Exception>(() => service.DeleteVehicleAsync(customer.CustomerID + 999, vehicle.VehicleId));

      Assert.StartsWith("NOT_FOUND", ex.Message);
    }

    [Fact]
    public async Task AddVehicleAsync_WithInvalidLicensePlateFormat_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var (service, customer) = CreateService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.AddVehicleAsync(customer.CustomerID, new AddVehicleRequest
      {
        LicensePlate = "AEDADAWDAWD"
      }));

      Assert.StartsWith("INVALID_LICENSE_PLATE", ex.Message);
    }

    [Fact]
    public async Task UpdateVehicleAsync_WithInvalidLicensePlateFormat_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var (service, customer) = CreateService(dbContext);

      var vehicle = await service.AddVehicleAsync(customer.CustomerID, new AddVehicleRequest { LicensePlate = "51A-123.45" });

      var ex = await Assert.ThrowsAsync<Exception>(() => service.UpdateVehicleAsync(customer.CustomerID, vehicle.VehicleId, new UpdateVehicleRequest
      {
        LicensePlate = "12345"
      }));

      Assert.StartsWith("INVALID_LICENSE_PLATE", ex.Message);
    }
  }
}
