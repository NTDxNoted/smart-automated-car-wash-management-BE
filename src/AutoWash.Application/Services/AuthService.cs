using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoWash.Application.DTOs;
using AutoWash.Application.Exceptions;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Entities;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace AutoWash.Application.Services
{
  public class AuthService : IAuthService
  {
    private readonly IApplicationDbContext _context;
    private readonly ILogger<AuthService> _logger;
    private readonly string _jwtSecret;
    private readonly int _jwtExpireDays = 7;

    public AuthService(
        IApplicationDbContext context,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
      _context = context;
      _logger = logger;

      _jwtSecret = configuration["Jwt:Secret"]
          ?? throw new InvalidOperationException("JWT secret is not configured.");
    }

    // ================= REGISTER =================
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
      if (string.IsNullOrWhiteSpace(request.Phone) ||
          string.IsNullOrWhiteSpace(request.Password) ||
          string.IsNullOrWhiteSpace(request.FullName))
      {
        throw new AuthException("INVALID_REQUEST", "Thông tin đăng ký không hợp lệ.");
      }

      var phone = request.Phone.Trim();

      if (await _context.Customers.AnyAsync(c => c.Phone == phone))
      {
        throw new AuthException("PHONE_EXISTS", "Số điện thoại đã tồn tại.");
      }

      var customer = new Customer
      {
        FullName = request.FullName.Trim(),
        Phone = phone,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        TierID = 1,
        IsLocked = false,
        CreatedAt = DateTime.UtcNow
      };

      _context.Customers.Add(customer);
      await _context.SaveChangesAsync();

      // optional: loyalty
      _context.LoyaltyAccounts.Add(new LoyaltyAccount
      {
        CustomerID = customer.CustomerID,
        TotalPoints = 0,
        LastUpdated = DateTime.UtcNow
      });

      await _context.SaveChangesAsync();

      return new AuthResponse
      {
        CustomerId = customer.CustomerID,
        FullName = customer.FullName,
        Phone = customer.Phone,
        Tier = "Member",
        IsLocked = customer.IsLocked,
        SuspendedUntil = customer.SuspendedUntil,
        CreatedAt = customer.CreatedAt
      };
    }

    // ================= LOGIN =================
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
      var phone = request.Phone?.Trim();

      var customer = await _context.Customers
          .SingleOrDefaultAsync(c => c.Phone == phone);

      if (customer == null ||
          !BCrypt.Net.BCrypt.Verify(request.Password, customer.PasswordHash))
      {
        throw new AuthException("INVALID_LOGIN", "Sai tài khoản hoặc mật khẩu.");
      }

      if (customer.IsLocked)
      {
        throw new AuthException("LOCKED", "Tài khoản bị khóa.");
      }

      var token = GenerateJwtToken(customer);

      return new AuthResponse
      {
        Token = token,
        CustomerId = customer.CustomerID,
        FullName = customer.FullName,
        Phone = customer.Phone,
        Tier = "Member",
        IsLocked = customer.IsLocked,
        SuspendedUntil = customer.SuspendedUntil
      };
    }

    // ================= JWT =================
    private string GenerateJwtToken(Customer customer)
    {
      var tokenHandler = new JwtSecurityTokenHandler();
      var key = Encoding.UTF8.GetBytes(_jwtSecret);

      var claims = new[]
      {
                new Claim(ClaimTypes.NameIdentifier, customer.CustomerID.ToString()),
                new Claim(ClaimTypes.Name, customer.FullName),
                new Claim(ClaimTypes.MobilePhone, customer.Phone),
                new Claim(ClaimTypes.Role, "Customer")
            };

      var tokenDescriptor = new SecurityTokenDescriptor
      {
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.UtcNow.AddDays(_jwtExpireDays),
        SigningCredentials = new SigningCredentials(
              new SymmetricSecurityKey(key),
              SecurityAlgorithms.HmacSha256)
      };

      return new JwtSecurityTokenHandler().WriteToken(
          tokenHandler.CreateToken(tokenDescriptor));
    }
  }
}