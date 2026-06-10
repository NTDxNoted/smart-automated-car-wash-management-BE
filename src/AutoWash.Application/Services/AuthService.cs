using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace AutoWash.Application.Services
{
  public class AuthService : IAuthService
  {
    private readonly IApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public AuthService(IApplicationDbContext dbContext, IConfiguration configuration)
    {
      _dbContext = dbContext;
      _configuration = configuration;
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
        Tier = "1", // Mặc định là Tier 1 khi đăng ký mới
        IsLocked = false,
        TotalSpending = 0m,
        CreatedAt = DateTime.UtcNow
      };

      _dbContext.Customers.Add(customer);
      await _dbContext.SaveChangesAsync();

      var loyaltyAccount = new LoyaltyAccount
      {
        CustomerID = customer.CustomerID,
        TotalPoints = 0,
        LastUpdated = DateTime.UtcNow
      };

      _dbContext.LoyaltyAccounts.Add(loyaltyAccount);
      await _dbContext.SaveChangesAsync();

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

    private string GenerateJwtToken(Customer customer)
    {
      var jwtSecretKey = _configuration["Jwt:SecretKey"];

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
      throw new NotImplementedException();
    }

    public int? GetCustomerIdFromToken(string token)
    {
      throw new NotImplementedException();
    }
  }
}