using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    private readonly IAdminNotifier? _adminNotifier;

    // adminNotifier default null để các unit test cũ (chỉ truyền dbContext + configuration) không phải sửa.
    public AuthService(IApplicationDbContext dbContext, IConfiguration configuration, IAdminNotifier? adminNotifier = null)
    {
      _dbContext = dbContext;
      _configuration = configuration;
      _adminNotifier = adminNotifier;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
      if (request == null)
        throw new ArgumentNullException(nameof(request));

      if (string.IsNullOrWhiteSpace(request.FullName))
        throw new ArgumentException("Họ tên không được để trống");

      if (string.IsNullOrWhiteSpace(request.Phone))
        throw new ArgumentException("Số điện thoại không được để trống");

      if (string.IsNullOrWhiteSpace(request.Password))
        throw new ArgumentException("Mật khẩu không được để trống");

      if (!string.IsNullOrWhiteSpace(request.ConfirmPassword) &&
          request.Password != request.ConfirmPassword)
        throw new ArgumentException("Mật khẩu xác nhận không khớp");

      var phone = request.Phone.Trim();

      var existingCustomer = await _dbContext.Customers
          .FirstOrDefaultAsync(c => c.Phone == phone);

      if (existingCustomer != null)
        throw new InvalidOperationException("PHONE_ALREADY_EXISTS");

      var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

      var customer = new Customer
      {
        FullName = request.FullName.Trim(),
        Phone = phone,
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

      return new RegisterResponse
      {
        CustomerId = customer.CustomerID,
        FullName = customer.FullName,
        Phone = customer.Phone,
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

      // Tier là [NotMapped] trên Customer nên phải resolve tên tier từ TierID mỗi lần load lại từ DB
      customer.Tier = await ResolveTierNameAsync(customer.TierID);

      var token = GenerateJwtToken(customer);

      return new AuthResponse
      {
        CustomerId = customer.CustomerID,
        FullName = customer.FullName,
        Phone = customer.Phone,
        Role = customer.Role,
        Tier = customer.Tier,
        Token = token,
        IsLocked = customer.IsLocked,
        SuspendedUntil = customer.SuspendedUntil,
        CreatedAt = customer.CreatedAt
      };
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

    private string GenerateJwtToken(Customer customer)
    {
      var jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? _configuration["Jwt:SecretKey"];

      if (string.IsNullOrWhiteSpace(jwtSecretKey))
        throw new InvalidOperationException("JWT SecretKey chưa được cấu hình");

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
                new Claim(ClaimTypes.Role, customer.Role)
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

    public Task<bool> ValidateTokenAsync(string token)
    {
      var jwtSecretKey = _configuration["Jwt:SecretKey"];
      if (string.IsNullOrWhiteSpace(jwtSecretKey))
        return Task.FromResult(false);

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