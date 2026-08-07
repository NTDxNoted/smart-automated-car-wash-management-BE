using AutoWash.Application.Common;
using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Entities;
using AutoWash.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace AutoWash.Application.Services
{
  public class AuthService : IAuthService
  {
    private readonly IApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly IOtpService _otpService;
    private readonly IAdminNotifier? _adminNotifier;
    private readonly ILogger<AuthService>? _logger;

    // adminNotifier/logger default null để các unit test cũ (chỉ truyền dbContext + configuration + otpService) không phải sửa.
    public AuthService(IApplicationDbContext dbContext, IConfiguration configuration, IOtpService otpService, IAdminNotifier? adminNotifier = null, ILogger<AuthService>? logger = null)
    {
      _dbContext = dbContext;
      _configuration = configuration;
      _otpService = otpService;
      _adminNotifier = adminNotifier;
      _logger = logger;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
      if (request == null)
        throw new ArgumentNullException(nameof(request));

      if (string.IsNullOrWhiteSpace(request.FullName))
        throw new ArgumentException("Họ tên không được để trống");

      if (string.IsNullOrWhiteSpace(request.Phone))
        throw new ArgumentException("Số điện thoại không được để trống");

      if (string.IsNullOrWhiteSpace(request.Email))
        throw new ArgumentException("Email không được để trống");

      if (string.IsNullOrWhiteSpace(request.Password))
        throw new ArgumentException("Mật khẩu không được để trống");

      if (!string.IsNullOrWhiteSpace(request.ConfirmPassword) &&
          request.Password != request.ConfirmPassword)
        throw new ArgumentException("Mật khẩu xác nhận không khớp");

      var phone = request.Phone.Trim();
      var email = request.Email.Trim().ToLowerInvariant();

      var existingCustomer = await _dbContext.Customers
          .FirstOrDefaultAsync(c => c.Phone == phone || c.Email == email);

      if (existingCustomer != null)
      {
        if (existingCustomer.Phone == phone)
          throw new InvalidOperationException("PHONE_ALREADY_EXISTS");

        throw new InvalidOperationException("EMAIL_ALREADY_EXISTS");
      }

      var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

      var customer = new Customer
      {
        FullName = request.FullName.Trim(),
        Phone = phone,
        Email = email,
        IsEmailVerified = false,
        Password = hashedPassword,
        Role = "MEMBER",
        IsLocked = false,
        TotalSpending = 0m,
        CreatedAt = DateTime.UtcNow
      };

      _dbContext.Customers.Add(customer);
      await _dbContext.SaveChangesAsync();

      // BR-14: mặc định TierID = 1 (Member) khi đăng ký mới
      customer.Tier = await ResolveTierNameAsync(customer.TierID);

      var loyaltyAccount = new LoyaltyAccount
      {
        CustomerID = customer.CustomerID,
        TotalPoints = 0,
        LastUpdated = DateTime.UtcNow
      };

      _dbContext.LoyaltyAccounts.Add(loyaltyAccount);
      await _dbContext.SaveChangesAsync();

      // Đẩy real-time cho Admin đang online — thay cho FE phải polling để phát hiện khách mới đăng ký.
      if (_adminNotifier != null)
      {
        await _adminNotifier.NotifyNewCustomerAsync(customer.CustomerID, customer.FullName, customer.Phone);
      }

      try
      {
        await _otpService.GenerateAndSendAsync(customer, OtpPurpose.RegisterVerify);
      }
      catch (Exception ex)
      {
        // Tài khoản đã được tạo thành công ở bước trên; gửi email chỉ là best-effort.
        // Một lỗi SMTP tạm thời không được phép làm hỏng cả request đăng ký (khách sẽ
        // không nhận được response thành công nhưng phone/email đã bị chiếm, không thể đăng ký lại).
        // Khách có thể gọi resend-verification-email để thử gửi lại.
        _logger?.LogWarning(ex, "Failed to send registration OTP email to {Email}", customer.Email);
      }

      return new RegisterResponse
      {
        CustomerId = customer.CustomerID,
        FullName = customer.FullName,
        Phone = customer.Phone,
        Email = customer.Email ?? string.Empty,
        Tier = customer.Tier,
        CreatedAt = customer.CreatedAt
      };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
      if (request == null)
        throw new ArgumentNullException(nameof(request));

      if (string.IsNullOrWhiteSpace(request.Phone))
        throw new ArgumentException("Số điện thoại không được để trống");

      if (string.IsNullOrWhiteSpace(request.Password))
        throw new ArgumentException("Mật khẩu không được để trống");

      var phone = request.Phone.Trim();

      var customer = await _dbContext.Customers
          .FirstOrDefaultAsync(c => c.Phone == phone);

      if (customer == null)
        throw new UnauthorizedAccessException("INVALID_CREDENTIALS");

      if (customer.IsLocked)
        throw new InvalidOperationException("ACCOUNT_LOCKED");

      if (!BCrypt.Net.BCrypt.Verify(request.Password, customer.Password))
        throw new UnauthorizedAccessException("INVALID_CREDENTIALS");

      if (!customer.IsEmailVerified)
        throw new InvalidOperationException("EMAIL_NOT_VERIFIED");

      if (customer.Is2FAEnabled)
      {
        await SendOtpOrThrowAsync(customer, OtpPurpose.Login2Fa);
        throw new TwoFactorRequiredException(MaskEmail(customer.Email ?? string.Empty));
      }

      return await IssueAuthResponseAsync(customer);
    }

    public async Task<AuthResponse> VerifyLoginOtpAsync(VerifyLoginOtpRequest request)
    {
      if (request == null)
        throw new ArgumentNullException(nameof(request));

      var phone = (request.Phone ?? string.Empty).Trim();

      var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Phone == phone);
      if (customer == null)
        throw new InvalidOperationException("OTP_NOT_FOUND");

      await _otpService.VerifyAsync(customer.CustomerID, OtpPurpose.Login2Fa, request.Code);

      return await IssueAuthResponseAsync(customer);
    }

    public async Task VerifyEmailAsync(VerifyEmailRequest request)
    {
      if (request == null)
        throw new ArgumentNullException(nameof(request));

      var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();

      var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Email == email);
      if (customer == null)
        throw new InvalidOperationException("CUSTOMER_NOT_FOUND");

      if (customer.IsEmailVerified)
        return;

      await _otpService.VerifyAsync(customer.CustomerID, OtpPurpose.RegisterVerify, request.Code);

      customer.IsEmailVerified = true;
      await _dbContext.SaveChangesAsync();
    }

    public async Task ResendVerificationEmailAsync(ResendOtpRequest request)
    {
      if (request == null)
        throw new ArgumentNullException(nameof(request));

      var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();

      var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Email == email);
      if (customer == null)
        throw new InvalidOperationException("CUSTOMER_NOT_FOUND");

      if (customer.IsEmailVerified)
        throw new InvalidOperationException("EMAIL_ALREADY_VERIFIED");

      await SendOtpOrThrowAsync(customer, OtpPurpose.RegisterVerify);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
      if (request == null)
        throw new ArgumentNullException(nameof(request));

      var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();

      var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Email == email);
      if (customer == null)
        return; // Không tiết lộ email có tồn tại hay không.

      await _otpService.GenerateAndSendAsync(customer, OtpPurpose.ResetPassword);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
      if (request == null)
        throw new ArgumentNullException(nameof(request));

      if (request.NewPassword != request.ConfirmNewPassword)
        throw new ArgumentException("Mật khẩu xác nhận không khớp");

      var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();

      var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Email == email);
      if (customer == null)
        throw new InvalidOperationException("OTP_NOT_FOUND");

      await _otpService.VerifyAsync(customer.CustomerID, OtpPurpose.ResetPassword, request.Code);

      customer.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
      customer.ActiveSessionId = null; // Buộc đăng nhập lại ở mọi phiên sau khi đổi mật khẩu.
      await _dbContext.SaveChangesAsync();
    }

    public async Task SetTwoFactorEnabledAsync(int customerId, bool enable)
    {
      var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.CustomerID == customerId);
      if (customer == null)
        throw new InvalidOperationException("CUSTOMER_NOT_FOUND");

      customer.Is2FAEnabled = enable;
      await _dbContext.SaveChangesAsync();
    }

    // Bọc lỗi gửi email (SMTP...) thành một InvalidOperationException gọn gàng để controller trả về
    // lỗi rõ ràng thay vì 500 chung chung — nhưng để nguyên OTP_COOLDOWN vì đó là lỗi nghiệp vụ có chủ đích.
    private async Task SendOtpOrThrowAsync(Customer customer, OtpPurpose purpose)
    {
      try
      {
        await _otpService.GenerateAndSendAsync(customer, purpose);
      }
      catch (InvalidOperationException ex) when (ex.Message == "OTP_COOLDOWN")
      {
        throw;
      }
      catch (Exception ex)
      {
        _logger?.LogWarning(ex, "Failed to send OTP email to {Email} for {Purpose}", customer.Email, purpose);
        throw new InvalidOperationException("OTP_SEND_FAILED");
      }
    }

    private async Task<AuthResponse> IssueAuthResponseAsync(Customer customer)
    {
      // Tier là [NotMapped] trên Customer nên phải resolve tên tier từ TierID mỗi lần load lại từ DB
      customer.Tier = await ResolveTierNameAsync(customer.TierID);
      customer.ActiveSessionId = Guid.NewGuid().ToString("N");
      await _dbContext.SaveChangesAsync();

      var token = GenerateJwtToken(customer);

      return new AuthResponse
      {
        CustomerId = customer.CustomerID,
        FullName = customer.FullName,
        Phone = customer.Phone,
        Email = customer.Email ?? string.Empty,
        Role = customer.Role,
        Tier = customer.Tier,
        Token = token,
        IsLocked = customer.IsLocked,
        SuspendedUntil = customer.SuspendedUntil,
        CreatedAt = customer.CreatedAt
      };
    }

    private static string MaskEmail(string email)
    {
      if (string.IsNullOrEmpty(email))
        return email;

      var atIndex = email.IndexOf('@');
      if (atIndex <= 1)
        return email;

      var visible = email.Substring(0, 2);
      return visible + new string('*', atIndex - 2) + email.Substring(atIndex);
    }

    // Customer.Tier là [NotMapped] — TierID mới là nguồn sự thật, cần resolve tên qua bảng Tiers.
    // Fallback "Member" khi TierID=1 chưa được seed (vd. trong unit test dùng in-memory DB rỗng).
    private async Task<string> ResolveTierNameAsync(int tierId)
    {
      var tierName = await _dbContext.Tiers
          .Where(t => t.TierID == tierId)
          .Select(t => t.TierName)
          .FirstOrDefaultAsync();

      return tierName ?? (tierId == 1 ? "Member" : tierId.ToString());
    }

    private string GetJwtSecretKey()
    {
      var jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? _configuration["Jwt:SecretKey"];
      if (string.IsNullOrWhiteSpace(jwtSecretKey))
        throw new InvalidOperationException("JWT SecretKey is not configured.");
      return jwtSecretKey;
    }

    private string GenerateJwtToken(Customer customer)
    {
      var jwtSecretKey = GetJwtSecretKey();
      var jwtIssuer = _configuration["Jwt:Issuer"] ?? "AutoWashAPI";
      var jwtAudience = _configuration["Jwt:Audience"] ?? "AutoWashClient";
      var jwtExpiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "1440");

      var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecretKey));
      var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

      var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, customer.CustomerID.ToString()),
                new Claim(ClaimTypes.Name, customer.FullName),
                new Claim("phone", customer.Phone),
                new Claim("tier", customer.Tier),
                new Claim(ClaimTypes.Role, customer.Role),
                new Claim("SessionId", customer.ActiveSessionId ?? string.Empty)
            };

      var token = new JwtSecurityToken(
          issuer: jwtIssuer,
          audience: jwtAudience,
          claims: claims,
          expires: DateTime.UtcNow.AddMinutes(jwtExpiryMinutes),
          signingCredentials: creds
      );

      return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task LogoutAsync(int customerId)
    {
      var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.CustomerID == customerId);
      if (customer == null)
        return;

      customer.ActiveSessionId = null;
      await _dbContext.SaveChangesAsync();
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
      string jwtSecretKey;
      try
      {
        jwtSecretKey = GetJwtSecretKey();
      }
      catch
      {
        return Task.FromResult(false);
      }

      var jwtIssuer = _configuration["Jwt:Issuer"] ?? "AutoWashAPI";
      var jwtAudience = _configuration["Jwt:Audience"] ?? "AutoWashClient";
      var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecretKey));

      try
      {
        new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
        {
          ValidateIssuerSigningKey = true,
          IssuerSigningKey = key,
          ValidateIssuer = true,
          ValidIssuer = jwtIssuer,
          ValidateAudience = true,
          ValidAudience = jwtAudience,
          ValidateLifetime = true,
          ClockSkew = TimeSpan.Zero
        }, out _);

        return Task.FromResult(true);
      }
      catch
      {
        return Task.FromResult(false);
      }
    }

    public int? GetCustomerIdFromToken(string token)
    {
      try
      {
        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var customerIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

        return customerIdClaim != null && int.TryParse(customerIdClaim.Value, out var customerId)
            ? customerId
            : null;
      }
      catch
      {
        return null;
      }
    }
  }
}
