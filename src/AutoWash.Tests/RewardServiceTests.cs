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
  public class RewardServiceTests
  {
    private static ApplicationDbContext CreateDbContext()
    {
      var options = new DbContextOptionsBuilder<ApplicationDbContext>()
          .UseInMemoryDatabase(Guid.NewGuid().ToString())
          .Options;

      return new ApplicationDbContext(options);
    }

    private static RewardService CreateService(ApplicationDbContext dbContext) =>
        new RewardService(dbContext, Mock.Of<ILogger<RewardService>>());

    [Fact]
    public async Task GetActiveRewardsAsync_ShouldOnlyReturnActiveRewards()
    {
      using var dbContext = CreateDbContext();
      dbContext.RewardsCatalog.Add(new RewardsCatalog { RewardName = "Active Reward", Description = "d", PointsRequired = 50, DiscountAmount = 10000m, IsActive = true });
      dbContext.RewardsCatalog.Add(new RewardsCatalog { RewardName = "Inactive Reward", Description = "d", PointsRequired = 50, DiscountAmount = 10000m, IsActive = false });
      await dbContext.SaveChangesAsync();

      var service = CreateService(dbContext);
      var rewards = (await service.GetActiveRewardsAsync()).ToList();

      Assert.Single(rewards);
      Assert.Equal("Active Reward", rewards[0].RewardName);
    }

    [Fact]
    public async Task CreateRewardAsync_WithValidData_ShouldPersistAndReturnActiveReward()
    {
      using var dbContext = CreateDbContext();
      var service = CreateService(dbContext);

      var result = await service.CreateRewardAsync(new CreateRewardRequest
      {
        RewardName = "Free Wash",
        Description = "Rửa xe miễn phí",
        PointsRequired = 100,
        DiscountAmount = 50000m
      });

      Assert.True(result.RewardId > 0);
      Assert.True(result.IsActive);
      Assert.Equal("Fixed_Amount", result.DiscountType); // mặc định khi không truyền
      Assert.Equal(1, await dbContext.RewardsCatalog.CountAsync());
    }

    [Fact]
    public async Task CreateRewardAsync_WithPercentageDiscountType_ShouldPersist()
    {
      using var dbContext = CreateDbContext();
      var service = CreateService(dbContext);

      var result = await service.CreateRewardAsync(new CreateRewardRequest
      {
        RewardName = "Giảm 10%",
        Description = "Giảm 10% giá dịch vụ",
        PointsRequired = 300,
        DiscountAmount = 10m,
        DiscountType = "Percentage"
      });

      Assert.Equal("Percentage", result.DiscountType);
      Assert.Equal(10m, result.DiscountAmount);
    }

    [Fact]
    public async Task CreateRewardAsync_WithInvalidDiscountType_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var service = CreateService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.CreateRewardAsync(new CreateRewardRequest
      {
        RewardName = "Reward",
        Description = "d",
        PointsRequired = 100,
        DiscountAmount = 10m,
        DiscountType = "Bitcoin"
      }));

      Assert.StartsWith("INVALID_REQUEST", ex.Message);
    }

    [Theory]
    [InlineData("", "desc", 10)]
    [InlineData("name", "", 10)]
    [InlineData("name", "desc", 0)]
    [InlineData("name", "desc", -5)]
    public async Task CreateRewardAsync_WithInvalidData_ShouldThrow(string name, string description, int pointsRequired)
    {
      using var dbContext = CreateDbContext();
      var service = CreateService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.CreateRewardAsync(new CreateRewardRequest
      {
        RewardName = name,
        Description = description,
        PointsRequired = pointsRequired,
        DiscountAmount = 10000m
      }));

      Assert.StartsWith("INVALID_REQUEST", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task CreateRewardAsync_WithNonPositiveDiscountAmount_ShouldThrow(int discountAmount)
    {
      using var dbContext = CreateDbContext();
      var service = CreateService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.CreateRewardAsync(new CreateRewardRequest
      {
        RewardName = "Reward",
        Description = "d",
        PointsRequired = 100,
        DiscountAmount = discountAmount
      }));

      Assert.StartsWith("INVALID_REQUEST", ex.Message);
    }

    [Fact]
    public async Task CreateRewardAsync_WithPercentageOver100_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var service = CreateService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.CreateRewardAsync(new CreateRewardRequest
      {
        RewardName = "Reward",
        Description = "d",
        PointsRequired = 100,
        DiscountAmount = 150m,
        DiscountType = "Percentage"
      }));

      Assert.StartsWith("INVALID_REQUEST", ex.Message);
    }

    [Fact]
    public async Task UpdateRewardAsync_WithNonPositiveDiscountAmount_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var reward = new RewardsCatalog { RewardName = "Reward", Description = "d", PointsRequired = 50, DiscountAmount = 10000m, IsActive = true };
      dbContext.RewardsCatalog.Add(reward);
      await dbContext.SaveChangesAsync();

      var service = CreateService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.UpdateRewardAsync(reward.RewardID, new UpdateRewardRequest
      {
        DiscountAmount = -1m
      }));

      Assert.StartsWith("INVALID_REQUEST", ex.Message);
      Assert.Equal(10000m, reward.DiscountAmount); // không bị thay đổi khi request không hợp lệ
    }

    [Fact]
    public async Task UpdateRewardAsync_RaisingDiscountAmountOverOnExistingPercentageReward_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var reward = new RewardsCatalog { RewardName = "Reward", Description = "d", PointsRequired = 50, DiscountAmount = 10m, DiscountType = "Percentage", IsActive = true };
      dbContext.RewardsCatalog.Add(reward);
      await dbContext.SaveChangesAsync();

      var service = CreateService(dbContext);

      // Chỉ đổi DiscountAmount, không đổi DiscountType — vẫn phải bị chặn vì loại hiện tại là Percentage.
      var ex = await Assert.ThrowsAsync<Exception>(() => service.UpdateRewardAsync(reward.RewardID, new UpdateRewardRequest
      {
        DiscountAmount = 200m
      }));

      Assert.StartsWith("INVALID_REQUEST", ex.Message);
    }

    [Fact]
    public async Task UpdateRewardAsync_WithPartialFields_ShouldOnlyChangeProvidedFields()
    {
      using var dbContext = CreateDbContext();
      var reward = new RewardsCatalog { RewardName = "Old", Description = "Old desc", PointsRequired = 50, DiscountAmount = 10000m, IsActive = true };
      dbContext.RewardsCatalog.Add(reward);
      await dbContext.SaveChangesAsync();

      var service = CreateService(dbContext);
      var result = await service.UpdateRewardAsync(reward.RewardID, new UpdateRewardRequest
      {
        RewardName = "New Name"
      });

      Assert.Equal("New Name", result.RewardName);
      Assert.Equal("Old desc", result.Description);
      Assert.Equal(50, result.PointsRequired);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task UpdateRewardAsync_WithNonPositivePointsRequired_ShouldThrow(int pointsRequired)
    {
      using var dbContext = CreateDbContext();
      var reward = new RewardsCatalog { RewardName = "Reward", Description = "d", PointsRequired = 50, DiscountAmount = 10000m, IsActive = true };
      dbContext.RewardsCatalog.Add(reward);
      await dbContext.SaveChangesAsync();

      var service = CreateService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.UpdateRewardAsync(reward.RewardID, new UpdateRewardRequest
      {
        PointsRequired = pointsRequired
      }));

      Assert.StartsWith("INVALID_REQUEST", ex.Message);
      Assert.Equal(50, reward.PointsRequired); // không bị thay đổi khi request không hợp lệ
    }

    [Fact]
    public async Task UpdateRewardAsync_WithNonExistentReward_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var service = CreateService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.UpdateRewardAsync(999, new UpdateRewardRequest { RewardName = "X" }));

      Assert.StartsWith("NOT_FOUND", ex.Message);
    }

    [Fact]
    public async Task ToggleRewardStatusAsync_ShouldFlipIsActive()
    {
      using var dbContext = CreateDbContext();
      var reward = new RewardsCatalog { RewardName = "Reward", Description = "d", PointsRequired = 50, DiscountAmount = 10000m, IsActive = true };
      dbContext.RewardsCatalog.Add(reward);
      await dbContext.SaveChangesAsync();

      var service = CreateService(dbContext);

      var afterFirstToggle = await service.ToggleRewardStatusAsync(reward.RewardID);
      Assert.False(afterFirstToggle.IsActive);

      var afterSecondToggle = await service.ToggleRewardStatusAsync(reward.RewardID);
      Assert.True(afterSecondToggle.IsActive);
    }

    [Fact]
    public async Task ToggleRewardStatusAsync_WithNonExistentReward_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var service = CreateService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.ToggleRewardStatusAsync(999));

      Assert.StartsWith("NOT_FOUND", ex.Message);
    }
  }
}
