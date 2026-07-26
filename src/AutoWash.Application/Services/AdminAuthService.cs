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
    public class AdminAuthService : IAdminAuthService
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public AdminAuthService(IApplicationDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        public async Task<AdminLoginResponse> LoginAsync(AdminLoginRequest request)
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

            // 401: Unauthorized for incorrect credentials or role not being Admin
            if (customer == null)
                throw new UnauthorizedAccessException("INVALID_CREDENTIALS");

            // Case-insensitive comparison for Role
            if (!string.Equals(customer.Role, "ADMIN", StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("INVALID_CREDENTIALS");

            // Check if account is locked
            if (customer.IsLocked)
                throw new UnauthorizedAccessException("ACCOUNT_LOCKED");

            // Verify Bcrypt password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, customer.Password))
                throw new UnauthorizedAccessException("INVALID_CREDENTIALS");

            customer.ActiveSessionId = Guid.NewGuid().ToString("N");
            await _dbContext.SaveChangesAsync();

            var token = GenerateJwtToken(customer);

            return new AdminLoginResponse
            {
                Token = token,
                AdminId = customer.CustomerID,
                FullName = customer.FullName,
                Role = "Admin"
            };
        }

        public async Task<AdminProfileResponse> GetProfileAsync(int adminId)
        {
            var customer = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.CustomerID == adminId);

            if (customer == null || !string.Equals(customer.Role, "ADMIN", StringComparison.OrdinalIgnoreCase))
                throw new KeyNotFoundException("ADMIN_NOT_FOUND");

            return new AdminProfileResponse
            {
                AdminId = customer.CustomerID,
                FullName = customer.FullName,
                Phone = customer.Phone,
                Role = "Admin",
                CreatedAt = customer.CreatedAt
            };
        }

        public async Task LogoutAsync(int adminId)
        {
            var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.CustomerID == adminId);
            if (customer == null)
                return;

            customer.ActiveSessionId = null;
            await _dbContext.SaveChangesAsync();
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
                new Claim(JwtRegisteredClaimNames.Sub, customer.CustomerID.ToString()),
                new Claim(ClaimTypes.NameIdentifier, customer.CustomerID.ToString()),
                new Claim(ClaimTypes.Name, customer.FullName),
                new Claim("role", "ADMIN"),
                new Claim(ClaimTypes.Role, "ADMIN"),
                new Claim("phone", customer.Phone),
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
    }
}
