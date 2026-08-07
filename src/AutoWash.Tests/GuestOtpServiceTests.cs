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
using System.Threading.Tasks;

namespace AutoWash.Tests.Application.Services
{
  public class GuestOtpServiceTests
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

    [Fact]
    public async Task GenerateAndSendAsync_ShouldCreateOtpRowAndSendEmail()
    {
      var dbContext = CreateDbContext();
      var emailService = new Mock<IEmailService>();
      var service = new GuestOtpService(dbContext, emailService.Object, CreateSettings());

      await service.GenerateAndSendAsync("Guest@Example.com", OtpPurpose.GuestBookingVerify);

      var otp = await dbContext.GuestEmailOtps.SingleAsync();
      Assert.Equal("guest@example.com", otp.Email); // normalized lowercase
      Assert.Equal(OtpPurpose.GuestBookingVerify, otp.Purpose);
      Assert.False(otp.IsUsed);
      Assert.Null(otp.VerifiedAt);

      emailService.Verify(x => x.SendOtpEmailAsync("guest@example.com", It.IsAny<string>(), It.IsAny<string>(), OtpPurpose.GuestBookingVerify), Times.Once);
    }

    [Fact]
    public async Task GenerateAndSendAsync_WithinCooldown_ShouldThrow()
    {
      var dbContext = CreateDbContext();
      var emailService = new Mock<IEmailService>();
      var service = new GuestOtpService(dbContext, emailService.Object, CreateSettings(cooldownSeconds: 60));

      await service.GenerateAndSendAsync("guest@example.com", OtpPurpose.GuestBookingVerify);

      var exception = await Assert.ThrowsAsync<InvalidOperationException>(
          () => service.GenerateAndSendAsync("guest@example.com", OtpPurpose.GuestBookingVerify));
      Assert.Equal("OTP_COOLDOWN", exception.Message);
    }

    [Fact]
    public async Task VerifyAsync_WithCorrectCode_ShouldSucceedAndSetVerifiedAt()
    {
      var dbContext = CreateDbContext();
      string? sentCode = null;
      var emailService = new Mock<IEmailService>();
      emailService.Setup(x => x.SendOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<OtpPurpose>()))
          .Callback<string, string, string, OtpPurpose>((_, _, code, _) => sentCode = code)
          .Returns(Task.CompletedTask);

      var service = new GuestOtpService(dbContext, emailService.Object, CreateSettings());
      await service.GenerateAndSendAsync("guest@example.com", OtpPurpose.GuestBookingVerify);
      Assert.NotNull(sentCode);

      await service.VerifyAsync("guest@example.com", OtpPurpose.GuestBookingVerify, sentCode!);

      var otp = await dbContext.GuestEmailOtps.SingleAsync();
      Assert.True(otp.IsUsed);
      Assert.NotNull(otp.VerifiedAt);
    }

    [Fact]
    public async Task VerifyAsync_WithWrongCode_ShouldThrowOtpInvalid()
    {
      var dbContext = CreateDbContext();
      var service = new GuestOtpService(dbContext, new Mock<IEmailService>().Object, CreateSettings());
      await service.GenerateAndSendAsync("guest@example.com", OtpPurpose.GuestBookingVerify);

      var exception = await Assert.ThrowsAsync<InvalidOperationException>(
          () => service.VerifyAsync("guest@example.com", OtpPurpose.GuestBookingVerify, "000000"));
      Assert.Equal("OTP_INVALID", exception.Message);
    }

    [Fact]
    public async Task VerifyAsync_WhenNoOtpRequested_ShouldThrowOtpNotFound()
    {
      var dbContext = CreateDbContext();
      var service = new GuestOtpService(dbContext, new Mock<IEmailService>().Object, CreateSettings());

      var exception = await Assert.ThrowsAsync<InvalidOperationException>(
          () => service.VerifyAsync("nobody@example.com", OtpPurpose.GuestBookingVerify, "123456"));
      Assert.Equal("OTP_NOT_FOUND", exception.Message);
    }

    [Fact]
    public async Task IsRecentlyVerifiedAsync_BeforeVerification_ShouldReturnFalse()
    {
      var dbContext = CreateDbContext();
      var service = new GuestOtpService(dbContext, new Mock<IEmailService>().Object, CreateSettings());
      await service.GenerateAndSendAsync("guest@example.com", OtpPurpose.GuestBookingVerify);

      var verified = await service.IsRecentlyVerifiedAsync("guest@example.com", OtpPurpose.GuestBookingVerify);

      Assert.False(verified);
    }

    [Fact]
    public async Task IsRecentlyVerifiedAsync_AfterVerification_ShouldReturnTrue()
    {
      var dbContext = CreateDbContext();
      string? sentCode = null;
      var emailService = new Mock<IEmailService>();
      emailService.Setup(x => x.SendOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<OtpPurpose>()))
          .Callback<string, string, string, OtpPurpose>((_, _, code, _) => sentCode = code)
          .Returns(Task.CompletedTask);
      var service = new GuestOtpService(dbContext, emailService.Object, CreateSettings());
      await service.GenerateAndSendAsync("guest@example.com", OtpPurpose.GuestBookingVerify);
      await service.VerifyAsync("guest@example.com", OtpPurpose.GuestBookingVerify, sentCode!);

      var verified = await service.IsRecentlyVerifiedAsync("guest@example.com", OtpPurpose.GuestBookingVerify);

      Assert.True(verified);
    }

    [Fact]
    public async Task IsRecentlyVerifiedAsync_ForDifferentEmail_ShouldReturnFalse()
    {
      var dbContext = CreateDbContext();
      string? sentCode = null;
      var emailService = new Mock<IEmailService>();
      emailService.Setup(x => x.SendOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<OtpPurpose>()))
          .Callback<string, string, string, OtpPurpose>((_, _, code, _) => sentCode = code)
          .Returns(Task.CompletedTask);
      var service = new GuestOtpService(dbContext, emailService.Object, CreateSettings());
      await service.GenerateAndSendAsync("guest@example.com", OtpPurpose.GuestBookingVerify);
      await service.VerifyAsync("guest@example.com", OtpPurpose.GuestBookingVerify, sentCode!);

      var verified = await service.IsRecentlyVerifiedAsync("someoneelse@example.com", OtpPurpose.GuestBookingVerify);

      Assert.False(verified);
    }
  }
}
