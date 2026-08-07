using AutoWash.Application.Services;
using AutoWash.Domain.Entities;
using AutoWash.Domain.Enums;
using AutoWash.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AutoWash.Tests.Application.Services
{
  public class PointServiceTests
  {
    private static ApplicationDbContext CreateDbContext()
    {
      var options = new DbContextOptionsBuilder<ApplicationDbContext>()
          .UseInMemoryDatabase(Guid.NewGuid().ToString())
          .Options;

      return new ApplicationDbContext(options);
    }

    private static PointService CreateService(ApplicationDbContext dbContext) =>
        new PointService(dbContext, Mock.Of<ILogger<PointService>>());

    private static Booking SeedBooking(ApplicationDbContext dbContext, decimal finalAmount, int customerId = 1)
    {
      var booking = new Booking
      {
        CustomerID = customerId,
        Phone = "0901234567",
        VehicleID = 1,
        LicensePlate = "51A-123.45",
        ServiceID = 1,
        ScheduledTime = DateTime.UtcNow,
        Status = BookingStatus.Completed,
        BaseAmount = finalAmount,
        FinalAmount = finalAmount,
        CreatedAt = DateTime.UtcNow
      };
      dbContext.Bookings.Add(booking);
      dbContext.SaveChanges();
      return booking;
    }

    [Fact]
    public async Task EarnPointsAsync_WithValidBooking_ShouldCreateLoyaltyAccountAndAddPoints()
    {
      using var dbContext = CreateDbContext();
      var booking = SeedBooking(dbContext, 250000m);
      var service = CreateService(dbContext);

      var points = await service.EarnPointsAsync(booking.BookingID);

      Assert.Equal(25, points);
      var loyalty = await dbContext.LoyaltyAccounts.FirstAsync(l => l.CustomerID == booking.CustomerID);
      Assert.Equal(25, loyalty.TotalPoints);
      var txn = await dbContext.PointTransactions.FirstAsync(t => t.LoyaltyID == loyalty.LoyaltyID);
      Assert.Equal(PointTransactionType.Earn, txn.Type);
      Assert.Equal(25, txn.Points);
    }

    [Fact]
    public async Task EarnPointsAsync_WithLargeAmount_ShouldCapAt500Points()
    {
      using var dbContext = CreateDbContext();
      var booking = SeedBooking(dbContext, 10_000_000m);
      var service = CreateService(dbContext);

      var points = await service.EarnPointsAsync(booking.BookingID);

      Assert.Equal(500, points);
    }

    [Fact]
    public async Task EarnPointsAsync_WithWalkInCustomer_ShouldNotCreditPoints()
    {
      using var dbContext = CreateDbContext();
      dbContext.Customers.Add(new Customer
      {
        CustomerID = 1,
        FullName = "Khách vãng lai",
        Phone = "0901234567",
        Password = "WALKIN",
        Role = "GUEST",
        CreatedAt = DateTime.UtcNow
      });
      dbContext.SaveChanges();
      var booking = SeedBooking(dbContext, 250000m);
      var service = CreateService(dbContext);

      var points = await service.EarnPointsAsync(booking.BookingID);

      Assert.Equal(0, points);
      Assert.Equal(0, booking.PointsEarned);
      Assert.False(await dbContext.LoyaltyAccounts.AnyAsync(l => l.CustomerID == booking.CustomerID));
    }

    [Fact]
    public async Task EarnPointsAsync_WithNonExistentBooking_ShouldReturnZero()
    {
      using var dbContext = CreateDbContext();
      var service = CreateService(dbContext);

      var points = await service.EarnPointsAsync(999);

      Assert.Equal(0, points);
    }

    [Fact]
    public async Task EarnPointsAsync_WithAmountBelow10000_ShouldEarnZeroAndNotCreateLoyaltyAccount()
    {
      using var dbContext = CreateDbContext();
      var booking = SeedBooking(dbContext, 5000m);
      var service = CreateService(dbContext);

      var points = await service.EarnPointsAsync(booking.BookingID);

      Assert.Equal(0, points);
      Assert.False(await dbContext.LoyaltyAccounts.AnyAsync(l => l.CustomerID == booking.CustomerID));
    }

    [Fact]
    public async Task GetWalletAsync_WithNoAccount_ShouldReturnEmptyNonRedeemableWallet()
    {
      using var dbContext = CreateDbContext();
      var service = CreateService(dbContext);

      var wallet = await service.GetWalletAsync(123);

      Assert.Equal(0, wallet.TotalPoints);
      Assert.False(wallet.CanRedeem);
      Assert.Empty(wallet.Batches);
    }

    [Fact]
    public async Task GetWalletAsync_WithAtLeast50Points_ShouldAllowRedeem()
    {
      using var dbContext = CreateDbContext();
      var account = new LoyaltyAccount { CustomerID = 1, TotalPoints = 50, LastUpdated = DateTime.UtcNow };
      dbContext.LoyaltyAccounts.Add(account);
      await dbContext.SaveChangesAsync();

      var service = CreateService(dbContext);
      var wallet = await service.GetWalletAsync(1);

      Assert.True(wallet.CanRedeem);
    }

    [Fact]
    public async Task GetWalletAsync_AfterPartialRedeem_ShouldComputeFifoBatches()
    {
      using var dbContext = CreateDbContext();
      var account = new LoyaltyAccount { CustomerID = 1, TotalPoints = 40, LastUpdated = DateTime.UtcNow };
      dbContext.LoyaltyAccounts.Add(account);
      await dbContext.SaveChangesAsync();

      var now = DateTime.UtcNow;
      dbContext.PointTransactions.Add(new PointTransaction
      {
        LoyaltyID = account.LoyaltyID,
        Points = 30,
        Type = PointTransactionType.Earn,
        CreatedAt = now.AddDays(-10),
        ExpiredAt = now.AddMonths(11)
      });
      dbContext.PointTransactions.Add(new PointTransaction
      {
        LoyaltyID = account.LoyaltyID,
        Points = 20,
        Type = PointTransactionType.Earn,
        CreatedAt = now.AddDays(-1),
        ExpiredAt = now.AddMonths(12)
      });
      dbContext.PointTransactions.Add(new PointTransaction
      {
        LoyaltyID = account.LoyaltyID,
        Points = -10,
        Type = PointTransactionType.Redeem,
        CreatedAt = now
      });
      await dbContext.SaveChangesAsync();

      var service = CreateService(dbContext);
      var wallet = await service.GetWalletAsync(1);

      var batches = wallet.Batches.ToList();
      Assert.Equal(2, batches.Count);
      Assert.Equal(20, batches[0].Points); // 30 - 10 đã tiêu, FIFO trừ vào batch cũ nhất trước
      Assert.Equal(20, batches[1].Points);
    }

    [Fact]
    public async Task GetWalletAsync_ShouldExcludeAlreadyExpiredEarnBatch()
    {
      using var dbContext = CreateDbContext();
      var account = new LoyaltyAccount { CustomerID = 1, TotalPoints = 10, LastUpdated = DateTime.UtcNow };
      dbContext.LoyaltyAccounts.Add(account);
      await dbContext.SaveChangesAsync();

      dbContext.PointTransactions.Add(new PointTransaction
      {
        LoyaltyID = account.LoyaltyID,
        Points = 10,
        Type = PointTransactionType.Earn,
        CreatedAt = DateTime.UtcNow.AddMonths(-13),
        ExpiredAt = DateTime.UtcNow.AddMonths(-1) // đã hết hạn
      });
      await dbContext.SaveChangesAsync();

      var service = CreateService(dbContext);
      var wallet = await service.GetWalletAsync(1);

      Assert.Empty(wallet.Batches);
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldReturnTransactionsNewestFirst()
    {
      using var dbContext = CreateDbContext();
      var account = new LoyaltyAccount { CustomerID = 1, TotalPoints = 20, LastUpdated = DateTime.UtcNow };
      dbContext.LoyaltyAccounts.Add(account);
      await dbContext.SaveChangesAsync();

      var now = DateTime.UtcNow;
      dbContext.PointTransactions.Add(new PointTransaction { LoyaltyID = account.LoyaltyID, Points = 10, Type = PointTransactionType.Earn, CreatedAt = now.AddDays(-2) });
      dbContext.PointTransactions.Add(new PointTransaction { LoyaltyID = account.LoyaltyID, Points = 10, Type = PointTransactionType.Earn, CreatedAt = now });
      await dbContext.SaveChangesAsync();

      var service = CreateService(dbContext);
      var history = (await service.GetHistoryAsync(1)).ToList();

      Assert.Equal(2, history.Count);
      Assert.True(history[0].CreatedAt > history[1].CreatedAt);
    }

    [Fact]
    public async Task GetHistoryAsync_WithNoAccount_ShouldReturnEmpty()
    {
      using var dbContext = CreateDbContext();
      var service = CreateService(dbContext);

      var history = await service.GetHistoryAsync(999);

      Assert.Empty(history);
    }

    [Fact]
    public async Task SimulateRedemptionAsync_ShouldCapDiscountAt50PercentOfBaseAmount()
    {
      using var dbContext = CreateDbContext();
      dbContext.RewardsCatalog.Add(new RewardsCatalog { RewardID = 1, RewardName = "Big Discount", PointsRequired = 50, DiscountAmount = 200000m, IsActive = true });
      dbContext.LoyaltyAccounts.Add(new LoyaltyAccount { CustomerID = 1, TotalPoints = 100, LastUpdated = DateTime.UtcNow });
      await dbContext.SaveChangesAsync();

      var service = CreateService(dbContext);
      var result = await service.SimulateRedemptionAsync(1, 1, baseAmount: 100000m);

      Assert.Equal(50000m, result.MaxAllowed);
      Assert.Equal(50000m, result.DiscountApplied); // 200k bị cap còn 50% của 100k
      Assert.Equal(50000m, result.FinalAmount);
      Assert.True(result.IsValid);
    }

    [Fact]
    public async Task SimulateRedemptionAsync_WithPercentageReward_ShouldComputeFromBaseAmount()
    {
      using var dbContext = CreateDbContext();
      dbContext.RewardsCatalog.Add(new RewardsCatalog { RewardID = 1, RewardName = "Giảm 10%", PointsRequired = 50, DiscountAmount = 10m, DiscountType = "Percentage", IsActive = true });
      dbContext.LoyaltyAccounts.Add(new LoyaltyAccount { CustomerID = 1, TotalPoints = 100, LastUpdated = DateTime.UtcNow });
      await dbContext.SaveChangesAsync();

      var service = CreateService(dbContext);
      var result = await service.SimulateRedemptionAsync(1, 1, baseAmount: 100000m);

      Assert.Equal("Percentage", result.DiscountType);
      Assert.Equal(10000m, result.DiscountApplied); // 10% của 100000, dưới ngưỡng 50% nên không bị cap
      Assert.Equal(90000m, result.FinalAmount);
    }

    [Fact]
    public async Task SimulateRedemptionAsync_WithPercentageRewardExceeding50Percent_ShouldCap()
    {
      using var dbContext = CreateDbContext();
      dbContext.RewardsCatalog.Add(new RewardsCatalog { RewardID = 1, RewardName = "Giảm 80%", PointsRequired = 50, DiscountAmount = 80m, DiscountType = "Percentage", IsActive = true });
      dbContext.LoyaltyAccounts.Add(new LoyaltyAccount { CustomerID = 1, TotalPoints = 100, LastUpdated = DateTime.UtcNow });
      await dbContext.SaveChangesAsync();

      var service = CreateService(dbContext);
      var result = await service.SimulateRedemptionAsync(1, 1, baseAmount: 100000m);

      Assert.Equal(50000m, result.DiscountApplied); // 80% = 80000, bị cap còn 50% = 50000 (BR-60)
    }

    [Fact]
    public async Task SimulateRedemptionAsync_WithInactiveOrMissingReward_ShouldThrow()
    {
      using var dbContext = CreateDbContext();
      var service = CreateService(dbContext);

      var ex = await Assert.ThrowsAsync<Exception>(() => service.SimulateRedemptionAsync(1, 999, 100000m));

      Assert.StartsWith("NOT_FOUND", ex.Message);
    }

    [Fact]
    public async Task SimulateRedemptionAsync_ShouldSubtractPointsLockedByPendingBookings()
    {
      using var dbContext = CreateDbContext();
      dbContext.RewardsCatalog.Add(new RewardsCatalog { RewardID = 1, RewardName = "Reward", PointsRequired = 50, DiscountAmount = 10000m, IsActive = true });
      dbContext.LoyaltyAccounts.Add(new LoyaltyAccount { CustomerID = 1, TotalPoints = 60, LastUpdated = DateTime.UtcNow });
      dbContext.Bookings.Add(new Booking
      {
        CustomerID = 1,
        Phone = "0901234567",
        VehicleID = 1,
        LicensePlate = "51A-123.45",
        ServiceID = 1,
        ScheduledTime = DateTime.UtcNow,
        Status = BookingStatus.Pending,
        PointsRedeemed = 20,
        CreatedAt = DateTime.UtcNow
      });
      await dbContext.SaveChangesAsync();

      var service = CreateService(dbContext);
      // 60 total - 20 locked = 40 available < 50 required => not valid
      var result = await service.SimulateRedemptionAsync(1, 1, 100000m);

      Assert.False(result.IsValid);
    }

    [Fact]
    public async Task RunDailyExpiryAsync_ShouldExpirePastDueEarnPointsAndReduceTotal()
    {
      using var dbContext = CreateDbContext();
      var account = new LoyaltyAccount { CustomerID = 1, TotalPoints = 30, LastUpdated = DateTime.UtcNow };
      dbContext.LoyaltyAccounts.Add(account);
      await dbContext.SaveChangesAsync();

      dbContext.PointTransactions.Add(new PointTransaction
      {
        LoyaltyID = account.LoyaltyID,
        Points = 30,
        Type = PointTransactionType.Earn,
        CreatedAt = DateTime.UtcNow.AddMonths(-13),
        ExpiredAt = DateTime.UtcNow.AddDays(-1)
      });
      await dbContext.SaveChangesAsync();

      var service = CreateService(dbContext);
      await service.RunDailyExpiryAsync();

      var persisted = await dbContext.LoyaltyAccounts.FirstAsync(a => a.LoyaltyID == account.LoyaltyID);
      Assert.Equal(0, persisted.TotalPoints);
      Assert.True(await dbContext.PointTransactions.AnyAsync(t => t.LoyaltyID == account.LoyaltyID && t.Type == PointTransactionType.Expire));
    }

    [Fact]
    public async Task RunDailyExpiryAsync_CalledTwice_ShouldBeIdempotent()
    {
      using var dbContext = CreateDbContext();
      var account = new LoyaltyAccount { CustomerID = 1, TotalPoints = 30, LastUpdated = DateTime.UtcNow };
      dbContext.LoyaltyAccounts.Add(account);
      await dbContext.SaveChangesAsync();

      dbContext.PointTransactions.Add(new PointTransaction
      {
        LoyaltyID = account.LoyaltyID,
        Points = 30,
        Type = PointTransactionType.Earn,
        CreatedAt = DateTime.UtcNow.AddMonths(-13),
        ExpiredAt = DateTime.UtcNow.AddDays(-1)
      });
      await dbContext.SaveChangesAsync();

      var service = CreateService(dbContext);
      await service.RunDailyExpiryAsync();
      await service.RunDailyExpiryAsync();

      var persisted = await dbContext.LoyaltyAccounts.FirstAsync(a => a.LoyaltyID == account.LoyaltyID);
      Assert.Equal(0, persisted.TotalPoints);

      var expireTxnCount = await dbContext.PointTransactions
          .CountAsync(t => t.LoyaltyID == account.LoyaltyID && t.Type == PointTransactionType.Expire);
      Assert.Equal(1, expireTxnCount);
    }
  }
}
