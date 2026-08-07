using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using AutoWash.Application.Common;
using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;
using AutoWash.Application.Services;
using AutoWash.Domain.Entities;
using AutoWash.Domain.Enums;
using AutoWash.Infrastructure.Data;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
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

    // Mặc định GenerateAndSendAsync/VerifyAsync no-op thành công — các test về OTP thật
    // (thành công/thất bại) nằm ở OtpServiceTests.cs; ở đây AuthService chỉ được test
    // như một black box gọi đúng IOtpService.
    private Mock<IOtpService> CreateOtpServiceMock()
    {
      var mock = new Mock<IOtpService>();
      mock.Setup(x => x.GenerateAndSendAsync(It.IsAny<Customer>(), It.IsAny<OtpPurpose>()))
          .Returns(Task.CompletedTask);
      mock.Setup(x => x.VerifyAsync(It.IsAny<int>(), It.IsAny<OtpPurpose>(), It.IsAny<string>()))
          .Returns(Task.CompletedTask);
      return mock;
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldCreateCustomerAndLoyaltyAccount()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var config = CreateConfiguration();
      var otpService = CreateOtpServiceMock();
      var authService = new AuthService(dbContext, config, otpService.Object);

      var registerRequest = new RegisterRequest
      {
        FullName = "Nguyễn Văn Test",
        Phone = "0901234567",
        Email = "test@example.com",
        Password = "Password123",
        ConfirmPassword = "Password123"
      };

      // Act
      var result = await authService.RegisterAsync(registerRequest);

      // Assert
      Assert.NotNull(result);
      Assert.Equal("Nguyễn Văn Test", result.FullName);
      Assert.Equal("0901234567", result.Phone);
      Assert.Equal("test@example.com", result.Email);
      Assert.Equal("Member", result.Tier);

      // Verify customer was created, unverified, and an OTP was sent
      var customer = await dbContext.Customers.FirstOrDefaultAsync(c => c.Phone == "0901234567");
      Assert.NotNull(customer);
      Assert.Equal("Nguyễn Văn Test", customer.FullName);
      Assert.False(customer.IsLocked);
      Assert.False(customer.IsEmailVerified);

      otpService.Verify(x => x.GenerateAndSendAsync(
          It.Is<Customer>(c => c.CustomerID == customer.CustomerID), OtpPurpose.RegisterVerify), Times.Once);

      // Verify loyalty account was created
      var loyaltyAccount = await dbContext.LoyaltyAccounts.FirstOrDefaultAsync(l => l.CustomerID == customer.CustomerID);
      Assert.NotNull(loyaltyAccount);
      Assert.Equal(0, loyaltyAccount.TotalPoints);
    }

    [Fact]
    public async Task RegisterAsync_WhenOtpEmailFails_ShouldStillCreateCustomerAndReturnSuccess()
    {
      // Arrange — SMTP lỗi (vd. chưa cấu hình App Password) không được phép làm hỏng cả request đăng ký,
      // vì tài khoản đã được tạo ở bước trước đó rồi (phone/email đã bị chiếm dù request có thất bại).
      var dbContext = CreateDbContext();
      var config = CreateConfiguration();
      var otpService = new Mock<IOtpService>();
      otpService.Setup(x => x.GenerateAndSendAsync(It.IsAny<Customer>(), It.IsAny<OtpPurpose>()))
          .ThrowsAsync(new InvalidOperationException("SMTP_FAILED"));
      var authService = new AuthService(dbContext, config, otpService.Object);

      var registerRequest = new RegisterRequest
      {
        FullName = "Nguyễn Văn Test",
        Phone = "0901234567",
        Email = "test@example.com",
        Password = "Password123",
        ConfirmPassword = "Password123"
      };

      // Act
      var result = await authService.RegisterAsync(registerRequest);

      // Assert
      Assert.NotNull(result);
      var customer = await dbContext.Customers.FirstOrDefaultAsync(c => c.Phone == "0901234567");
      Assert.NotNull(customer);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicatePhone_ShouldThrowInvalidOperationException()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var config = CreateConfiguration();
      var authService = new AuthService(dbContext, config, CreateOtpServiceMock().Object);

      // Create existing customer
      var existingCustomer = new Customer
      {
        FullName = "Existing User",
        Phone = "0901234567",
        Email = "existing@example.com",
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
        Email = "new@example.com",
        Password = "Password123",
        ConfirmPassword = "Password123"
      };

      // Act & Assert
      var exception = await Assert.ThrowsAsync<InvalidOperationException>(
          () => authService.RegisterAsync(registerRequest));
      Assert.Equal("PHONE_ALREADY_EXISTS", exception.Message);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ShouldThrowInvalidOperationException()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var config = CreateConfiguration();
      var authService = new AuthService(dbContext, config, CreateOtpServiceMock().Object);

      var existingCustomer = new Customer
      {
        FullName = "Existing User",
        Phone = "0900000000",
        Email = "dup@example.com",
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
        Email = "dup@example.com",
        Password = "Password123",
        ConfirmPassword = "Password123"
      };

      // Act & Assert
      var exception = await Assert.ThrowsAsync<InvalidOperationException>(
          () => authService.RegisterAsync(registerRequest));
      Assert.Equal("EMAIL_ALREADY_EXISTS", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnAuthResponse()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var config = CreateConfiguration();
      var authService = new AuthService(dbContext, config, CreateOtpServiceMock().Object);

      var password = "Password123";
      var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

      var customer = new Customer
      {
        FullName = "Test User",
        Phone = "0901234567",
        Email = "test@example.com",
        IsEmailVerified = true,
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
      Assert.Equal("test@example.com", result.Email);
      Assert.Equal("Member", result.Tier);
      Assert.NotEmpty(result.Token);
      Assert.False(result.IsLocked);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldPersistSessionIdAndEmitClaim()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var config = CreateConfiguration();
      var authService = new AuthService(dbContext, config, CreateOtpServiceMock().Object);

      var password = "Password123";
      var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

      var customer = new Customer
      {
        FullName = "Test User",
        Phone = "0901234567",
        Email = "test@example.com",
        IsEmailVerified = true,
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
      var persistedCustomer = await dbContext.Customers.FirstAsync(c => c.CustomerID == customer.CustomerID);
      Assert.False(string.IsNullOrWhiteSpace(persistedCustomer.ActiveSessionId));

      var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
      var sessionClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "SessionId");
      Assert.NotNull(sessionClaim);
      Assert.Equal(persistedCustomer.ActiveSessionId, sessionClaim!.Value);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldThrowUnauthorizedAccessException()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var config = CreateConfiguration();
      var authService = new AuthService(dbContext, config, CreateOtpServiceMock().Object);

      var hashedPassword = BCrypt.Net.BCrypt.HashPassword("CorrectPassword");

      var customer = new Customer
      {
        FullName = "Test User",
        Phone = "0901234567",
        Email = "test@example.com",
        IsEmailVerified = true,
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
      var authService = new AuthService(dbContext, config, CreateOtpServiceMock().Object);

      var password = "Password123";
      var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

      var customer = new Customer
      {
        FullName = "Test User",
        Phone = "0901234567",
        Email = "test@example.com",
        IsEmailVerified = true,
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
      var authService = new AuthService(dbContext, config, CreateOtpServiceMock().Object);

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
    public async Task LoginAsync_WithUnverifiedEmail_ShouldThrowInvalidOperationException()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var config = CreateConfiguration();
      var authService = new AuthService(dbContext, config, CreateOtpServiceMock().Object);

      var password = "Password123";
      var customer = new Customer
      {
        FullName = "Test User",
        Phone = "0901234567",
        Email = "test@example.com",
        IsEmailVerified = false, // chưa xác thực email
        Password = BCrypt.Net.BCrypt.HashPassword(password),
        Tier = "1",
        IsLocked = false,
        CreatedAt = DateTime.UtcNow
      };

      dbContext.Customers.Add(customer);
      await dbContext.SaveChangesAsync();

      var loginRequest = new LoginRequest { Phone = "0901234567", Password = password };

      // Act & Assert
      var exception = await Assert.ThrowsAsync<InvalidOperationException>(
          () => authService.LoginAsync(loginRequest));
      Assert.Equal("EMAIL_NOT_VERIFIED", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_With2FAEnabled_ShouldThrowTwoFactorRequiredExceptionAndSendOtp()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var config = CreateConfiguration();
      var otpService = CreateOtpServiceMock();
      var authService = new AuthService(dbContext, config, otpService.Object);

      var password = "Password123";
      var customer = new Customer
      {
        FullName = "Test User",
        Phone = "0901234567",
        Email = "test@example.com",
        IsEmailVerified = true,
        Is2FAEnabled = true,
        Password = BCrypt.Net.BCrypt.HashPassword(password),
        Tier = "1",
        IsLocked = false,
        CreatedAt = DateTime.UtcNow
      };

      dbContext.Customers.Add(customer);
      await dbContext.SaveChangesAsync();

      var loginRequest = new LoginRequest { Phone = "0901234567", Password = password };

      // Act & Assert
      var exception = await Assert.ThrowsAsync<TwoFactorRequiredException>(
          () => authService.LoginAsync(loginRequest));
      Assert.Equal("te**@example.com", exception.MaskedEmail);

      otpService.Verify(x => x.GenerateAndSendAsync(
          It.Is<Customer>(c => c.CustomerID == customer.CustomerID), OtpPurpose.Login2Fa), Times.Once);

      // Không được cấp session/JWT khi còn chờ OTP
      var persisted = await dbContext.Customers.FirstAsync(c => c.CustomerID == customer.CustomerID);
      Assert.Null(persisted.ActiveSessionId);
    }

    [Fact]
    public async Task VerifyLoginOtpAsync_WithValidOtp_ShouldReturnAuthResponse()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var config = CreateConfiguration();
      var otpService = CreateOtpServiceMock();
      var authService = new AuthService(dbContext, config, otpService.Object);

      var customer = new Customer
      {
        FullName = "Test User",
        Phone = "0901234567",
        Email = "test@example.com",
        IsEmailVerified = true,
        Is2FAEnabled = true,
        Password = BCrypt.Net.BCrypt.HashPassword("Password123"),
        Tier = "1",
        IsLocked = false,
        CreatedAt = DateTime.UtcNow
      };

      dbContext.Customers.Add(customer);
      await dbContext.SaveChangesAsync();

      var request = new VerifyLoginOtpRequest { Phone = "0901234567", Code = "123456" };

      // Act
      var result = await authService.VerifyLoginOtpAsync(request);

      // Assert
      Assert.NotNull(result);
      Assert.NotEmpty(result.Token);
      otpService.Verify(x => x.VerifyAsync(customer.CustomerID, OtpPurpose.Login2Fa, "123456"), Times.Once);
    }

    [Fact]
    public async Task VerifyEmailAsync_WithValidOtp_ShouldMarkCustomerVerified()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var config = CreateConfiguration();
      var otpService = CreateOtpServiceMock();
      var authService = new AuthService(dbContext, config, otpService.Object);

      var customer = new Customer
      {
        FullName = "Test User",
        Phone = "0901234567",
        Email = "test@example.com",
        IsEmailVerified = false,
        Password = BCrypt.Net.BCrypt.HashPassword("Password123"),
        Tier = "1",
        CreatedAt = DateTime.UtcNow
      };
      dbContext.Customers.Add(customer);
      await dbContext.SaveChangesAsync();

      // Act
      await authService.VerifyEmailAsync(new VerifyEmailRequest { Email = "test@example.com", Code = "123456" });

      // Assert
      var persisted = await dbContext.Customers.FirstAsync(c => c.CustomerID == customer.CustomerID);
      Assert.True(persisted.IsEmailVerified);
      otpService.Verify(x => x.VerifyAsync(customer.CustomerID, OtpPurpose.RegisterVerify, "123456"), Times.Once);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WithUnknownEmail_ShouldNotThrowOrSendOtp()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var config = CreateConfiguration();
      var otpService = CreateOtpServiceMock();
      var authService = new AuthService(dbContext, config, otpService.Object);

      // Act
      await authService.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "unknown@example.com" });

      // Assert — không tiết lộ email tồn tại hay không, và không gửi OTP
      otpService.Verify(x => x.GenerateAndSendAsync(It.IsAny<Customer>(), It.IsAny<OtpPurpose>()), Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidOtp_ShouldUpdatePasswordAndClearSession()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var config = CreateConfiguration();
      var otpService = CreateOtpServiceMock();
      var authService = new AuthService(dbContext, config, otpService.Object);

      var oldHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123");
      var customer = new Customer
      {
        FullName = "Test User",
        Phone = "0901234567",
        Email = "test@example.com",
        IsEmailVerified = true,
        Password = oldHash,
        Tier = "1",
        ActiveSessionId = "some-session",
        CreatedAt = DateTime.UtcNow
      };
      dbContext.Customers.Add(customer);
      await dbContext.SaveChangesAsync();

      var request = new ResetPasswordRequest
      {
        Email = "test@example.com",
        Code = "123456",
        NewPassword = "NewPassword123",
        ConfirmNewPassword = "NewPassword123"
      };

      // Act
      await authService.ResetPasswordAsync(request);

      // Assert
      var persisted = await dbContext.Customers.FirstAsync(c => c.CustomerID == customer.CustomerID);
      Assert.NotEqual(oldHash, persisted.Password);
      Assert.True(BCrypt.Net.BCrypt.Verify("NewPassword123", persisted.Password));
      Assert.Null(persisted.ActiveSessionId);
      otpService.Verify(x => x.VerifyAsync(customer.CustomerID, OtpPurpose.ResetPassword, "123456"), Times.Once);
    }

    [Fact]
    public async Task SetTwoFactorEnabledAsync_ShouldToggleFlag()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var config = CreateConfiguration();
      var authService = new AuthService(dbContext, config, CreateOtpServiceMock().Object);

      var customer = new Customer
      {
        FullName = "Test User",
        Phone = "0901234567",
        Email = "test@example.com",
        Password = BCrypt.Net.BCrypt.HashPassword("Password123"),
        Tier = "1",
        CreatedAt = DateTime.UtcNow
      };
      dbContext.Customers.Add(customer);
      await dbContext.SaveChangesAsync();

      // Act
      await authService.SetTwoFactorEnabledAsync(customer.CustomerID, true);

      // Assert
      var persisted = await dbContext.Customers.FirstAsync(c => c.CustomerID == customer.CustomerID);
      Assert.True(persisted.Is2FAEnabled);
    }

    [Fact]
    public async Task GetCustomerIdFromToken_WithValidToken_ShouldReturnCustomerId()
    {
      // Arrange
      var dbContext = CreateDbContext();
      var config = CreateConfiguration();
      var authService = new AuthService(dbContext, config, CreateOtpServiceMock().Object);

      var password = "Password123";
      var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

      var customer = new Customer
      {
        FullName = "Test User",
        Phone = "0901234567",
        Email = "test@example.com",
        IsEmailVerified = true,
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
      var authService = new AuthService(dbContext, config, CreateOtpServiceMock().Object);

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
      var authService = new AuthService(dbContext, config, CreateOtpServiceMock().Object);

      var password = "Password123";
      var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

      var customer = new Customer
      {
        FullName = "Test User",
        Phone = "0901234567",
        Email = "test@example.com",
        IsEmailVerified = true,
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
      var authService = new AuthService(dbContext, config, CreateOtpServiceMock().Object);

      // Act
      var isValid = await authService.ValidateTokenAsync("invalid.token.here");

      // Assert
      Assert.False(isValid);
    }
  }
}
