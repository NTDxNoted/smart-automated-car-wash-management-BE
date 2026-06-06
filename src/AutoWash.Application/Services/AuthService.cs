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
      _jwtSecret = configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT secret is not configured.");
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
      if (string.IsNullOrWhiteSpace(request.Phone) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.FullName))
      {
        throw new AuthException("INVALID_REQUEST", "Thông tin đăng ký không hợp lệ.");
      }

      if (await _context.Customers.AnyAsync(c => c.Phone == request.Phone.Trim()))
      {
        throw new AuthException("PHONE_ALREADY_EXISTS", "Số điện thoại đã được đăng ký");
      }

      var customer = new Customer
      {
        FullName = request.FullName.Trim(),
        Phone = request.Phone.Trim(),
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        TierID = 1,
        IsLocked = false,
        CreatedAt = DateTime.UtcNow
      };

      _context.Customers.Add(customer);
      await _context.SaveChangesAsync();

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
        Tier = customer.Tier,
        IsLocked = customer.IsLocked,
        SuspendedUntil = customer.SuspendedUntil,
        CreatedAt = customer.CreatedAt
      };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
      if (string.IsNullOrWhiteSpace(request.Phone) || string.IsNullOrWhiteSpace(request.Password))
      {
        throw new AuthException("INVALID_REQUEST", "Thông tin đăng nhập không hợp lệ.");
      }

      var customer = await _context.Customers.SingleOrDefaultAsync(c => c.Phone == request.Phone.Trim());
      if (customer == null || !BCrypt.Net.BCrypt.Verify(request.Password, customer.PasswordHash))
      {
        throw new AuthException("INVALID_CREDENTIALS", "Số điện thoại hoặc mật khẩu không đúng");
      }

      if (customer.IsLocked)
      {
        throw new AuthException("ACCOUNT_LOCKED", "Tài khoản đã bị khóa, vui lòng liên hệ Admin");
      }

      var token = GenerateJwtToken(customer);

      return new AuthResponse
      {
        Token = token,
        CustomerId = customer.CustomerID,
        FullName = customer.FullName,
        Phone = customer.Phone,
        Tier = customer.Tier,
        IsLocked = customer.IsLocked,
        SuspendedUntil = customer.SuspendedUntil
      };
    }

    private string GenerateJwtToken(Customer customer)
    {
      var tokenHandler = new JwtSecurityTokenHandler();
      var key = Encoding.ASCII.GetBytes(_jwtSecret);
      var claims = new[]
      {
                new Claim(ClaimTypes.NameIdentifier, customer.CustomerID.ToString()),
                new Claim(ClaimTypes.Name, customer.FullName),
                new Claim(ClaimTypes.MobilePhone, customer.Phone),
                new Claim(ClaimTypes.Role, customer.Role)
            };

      var tokenDescriptor = new SecurityTokenDescriptor
      {
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.UtcNow.AddDays(_jwtExpireDays),
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
      };

      var token = tokenHandler.CreateToken(tokenDescriptor);
      return tokenHandler.WriteToken(token);
    }
  }
}
