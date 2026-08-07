using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;
using AutoWash.Application.Services;
using AutoWash.Domain.Entities;
using AutoWash.Domain.Enums;
using AutoWash.Infrastructure.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AutoWash.Tests.Application.Services
{
  public class OtpServiceTests
  {
    private ApplicationDbContext CreateDbContext()
    {
      var options = new DbContextOptionsBuilder<ApplicationDbContext>()
          .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
          .Options;

      return new ApplicationDbContext(options);
    }

    private static IOptions<OtpSettings> CreateSettings(int expiryMinutes = 5, int maxAttempts = 3, int cooldownSeconds = 60)
    {
      return Options.Create(new OtpSettings
      {
        ExpiryMinutes = expiryMinutes,
        MaxAttempts = maxAttempts,
        ResendCooldownSeconds = cooldownSeconds
      });
    }

    private static Customer CreateCustomer(int id = 1) => new Customer
    {
      CustomerID = id,
      FullName = "Test User",
      Phone = "0901234567",
      Email = "test@example.com",
      CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task GenerateAndSendAsync_ShouldCreateOtpRowAndSendEmail()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var customer = CreateCustomer();
      dbContext.Customers.Add(customer);
      await dbContext.SaveChangesAsync();

      var emailService = new Mock<IEmailService>();
      var otpService = new OtpService(dbContext, emailService.Object, CreateSettings());

      // Act
      await otpService.GenerateAndSendAsync(customer, OtpPurpose.RegisterVerify);

      // Assert
      var otp = await dbContext.EmailOtps.SingleAsync(o => o.CustomerID == customer.CustomerID);
      Assert.Equal(OtpPurpose.RegisterVerify, otp.Purpose);
      Assert.False(otp.IsUsed);
      Assert.Equal(0, otp.Attempts);
      Assert.True(otp.ExpiresAt > DateTime.UtcNow);

      emailService.Verify(x => x.SendOtpEmailAsync(customer.Email, customer.FullName, It.IsAny<string>(), OtpPurpose.RegisterVerify), Times.Once);
    }

    [Fact]
    public async Task GenerateAndSendAsync_WithinCooldown_ShouldThrowInvalidOperationException()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var customer = CreateCustomer();
      dbContext.Customers.Add(customer);
      await dbContext.SaveChangesAsync();

      var emailService = new Mock<IEmailService>();
      var otpService = new OtpService(dbContext, emailService.Object, CreateSettings(cooldownSeconds: 60));

      await otpService.GenerateAndSendAsync(customer, OtpPurpose.RegisterVerify);

      // Act & Assert — gọi lại ngay lập tức trong lúc cooldown
      var exception = await Assert.ThrowsAsync<InvalidOperationException>(
          () => otpService.GenerateAndSendAsync(customer, OtpPurpose.RegisterVerify));
      Assert.Equal("OTP_COOLDOWN", exception.Message);

      // Chỉ có đúng 1 email được gửi
      emailService.Verify(x => x.SendOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<OtpPurpose>()), Times.Once);
    }

    [Fact]
    public async Task GenerateAndSendAsync_ShouldInvalidatePreviousUnusedOtp()
    {
      // Arrange — cooldown = 0 để có thể request lại ngay trong test
      var dbContext = CreateDbContext();
      var customer = CreateCustomer();
      dbContext.Customers.Add(customer);
      await dbContext.SaveChangesAsync();

      var emailService = new Mock<IEmailService>();
      var otpService = new OtpService(dbContext, emailService.Object, CreateSettings(cooldownSeconds: 0));

      await otpService.GenerateAndSendAsync(customer, OtpPurpose.RegisterVerify);
      var firstOtp = await dbContext.EmailOtps.SingleAsync(o => o.CustomerID == customer.CustomerID);

      // Act
      await otpService.GenerateAndSendAsync(customer, OtpPurpose.RegisterVerify);

      // Assert
      var reloadedFirst = await dbContext.EmailOtps.FirstAsync(o => o.OtpID == firstOtp.OtpID);
      Assert.True(reloadedFirst.IsUsed);

      var activeCount = await dbContext.EmailOtps.CountAsync(o => o.CustomerID == customer.CustomerID && !o.IsUsed);
      Assert.Equal(1, activeCount);
    }

    [Fact]
    public async Task VerifyAsync_WithCorrectCode_ShouldSucceedAndMarkUsed()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var customer = CreateCustomer();
      dbContext.Customers.Add(customer);
      await dbContext.SaveChangesAsync();

      string? sentCode = null;
      var emailService = new Mock<IEmailService>();
      emailService.Setup(x => x.SendOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<OtpPurpose>()))
          .Callback<string, string, string, OtpPurpose>((_, _, code, _) => sentCode = code)
          .Returns(Task.CompletedTask);

      var otpService = new OtpService(dbContext, emailService.Object, CreateSettings());
      await otpService.GenerateAndSendAsync(customer, OtpPurpose.RegisterVerify);
      Assert.NotNull(sentCode);

      // Act
      await otpService.VerifyAsync(customer.CustomerID, OtpPurpose.RegisterVerify, sentCode!);

      // Assert
      var otp = await dbContext.EmailOtps.SingleAsync(o => o.CustomerID == customer.CustomerID);
      Assert.True(otp.IsUsed);
    }

    [Fact]
    public async Task VerifyAsync_WithWrongCode_ShouldThrowOtpInvalidAndIncrementAttempts()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var customer = CreateCustomer();
      dbContext.Customers.Add(customer);
      await dbContext.SaveChangesAsync();

      var emailService = new Mock<IEmailService>();
      var otpService = new OtpService(dbContext, emailService.Object, CreateSettings(maxAttempts: 3));
      await otpService.GenerateAndSendAsync(customer, OtpPurpose.RegisterVerify);

      // Act & Assert
      var exception = await Assert.ThrowsAsync<InvalidOperationException>(
          () => otpService.VerifyAsync(customer.CustomerID, OtpPurpose.RegisterVerify, "000000"));
      Assert.Equal("OTP_INVALID", exception.Message);

      var otp = await dbContext.EmailOtps.SingleAsync(o => o.CustomerID == customer.CustomerID);
      Assert.Equal(1, otp.Attempts);
      Assert.False(otp.IsUsed);
    }

    [Fact]
    public async Task VerifyAsync_AfterMaxAttempts_ShouldThrowOtpLocked()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var customer = CreateCustomer();
      dbContext.Customers.Add(customer);
      await dbContext.SaveChangesAsync();

      var emailService = new Mock<IEmailService>();
      var otpService = new OtpService(dbContext, emailService.Object, CreateSettings(maxAttempts: 2));
      await otpService.GenerateAndSendAsync(customer, OtpPurpose.RegisterVerify);

      // 2 lần sai đầu tiên -> OTP_INVALID, lần thứ 2 tự khóa
      await Assert.ThrowsAsync<InvalidOperationException>(
          () => otpService.VerifyAsync(customer.CustomerID, OtpPurpose.RegisterVerify, "000000"));
      var second = await Assert.ThrowsAsync<InvalidOperationException>(
          () => otpService.VerifyAsync(customer.CustomerID, OtpPurpose.RegisterVerify, "000000"));
      Assert.Equal("OTP_INVALID", second.Message);

      // Act & Assert — lần thứ 3 không còn OTP nào active (đã bị khóa) -> OTP_NOT_FOUND
      var third = await Assert.ThrowsAsync<InvalidOperationException>(
          () => otpService.VerifyAsync(customer.CustomerID, OtpPurpose.RegisterVerify, "000000"));
      Assert.Equal("OTP_NOT_FOUND", third.Message);
    }

    [Fact]
    public async Task VerifyAsync_WhenExpired_ShouldThrowOtpExpired()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var customer = CreateCustomer();
      dbContext.Customers.Add(customer);
      await dbContext.SaveChangesAsync();

      dbContext.EmailOtps.Add(new EmailOtp
      {
        CustomerID = customer.CustomerID,
        Email = customer.Email,
        Purpose = OtpPurpose.RegisterVerify,
        CodeHash = BCrypt.Net.BCrypt.HashPassword("123456"),
        Attempts = 0,
        IsUsed = false,
        ExpiresAt = DateTime.UtcNow.AddMinutes(-1), // đã hết hạn
        CreatedAt = DateTime.UtcNow.AddMinutes(-10)
      });
      await dbContext.SaveChangesAsync();

      var otpService = new OtpService(dbContext, new Mock<IEmailService>().Object, CreateSettings());

      // Act & Assert
      var exception = await Assert.ThrowsAsync<InvalidOperationException>(
          () => otpService.VerifyAsync(customer.CustomerID, OtpPurpose.RegisterVerify, "123456"));
      Assert.Equal("OTP_EXPIRED", exception.Message);
    }

    [Fact]
    public async Task VerifyAsync_WhenNoOtpRequested_ShouldThrowOtpNotFound()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var otpService = new OtpService(dbContext, new Mock<IEmailService>().Object, CreateSettings());

      // Act & Assert
      var exception = await Assert.ThrowsAsync<InvalidOperationException>(
          () => otpService.VerifyAsync(999, OtpPurpose.RegisterVerify, "123456"));
      Assert.Equal("OTP_NOT_FOUND", exception.Message);
    }
  }
}
