using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AutoWashPro.API.Middleware
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _jwtKey;
        private readonly IApplicationDbContext _dbContext;

        public JwtMiddleware(RequestDelegate next, IConfiguration configuration, IApplicationDbContext dbContext)
        {
            _next = next;
            _jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? configuration["Jwt:SecretKey"]
                ?? throw new InvalidOperationException("Jwt:SecretKey is not configured.");
            _dbContext = dbContext;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? "";
            if (path.StartsWith("/api/auth/login", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/api/auth/register", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/api/admin/auth/login", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (!string.IsNullOrWhiteSpace(token))
            {
                var attached = await AttachUserAsync(context, token);
                if (!attached)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "SESSION_EXPIRED",
                        message = "Tài khoản của bạn đã được đăng nhập từ một thiết bị khác. Vui lòng đăng nhập lại."
                    }, cancellationToken: context.RequestAborted);
                    return;
                }
            }

            await _next(context);
        }

        private async Task<bool> AttachUserAsync(HttpContext context, string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtKey);
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var sessionId = jwtToken.Claims.FirstOrDefault(c => c.Type == "SessionId")?.Value;
                var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == JwtRegisteredClaimNames.Sub)?.Value;

                if (string.IsNullOrWhiteSpace(sessionId) || !int.TryParse(userIdClaim, out var userId))
                {
                    return false;
                }

                var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.CustomerID == userId, context.RequestAborted);
                if (customer == null || string.IsNullOrWhiteSpace(customer.ActiveSessionId))
                {
                    return false;
                }

                if (!string.Equals(customer.ActiveSessionId, sessionId, StringComparison.Ordinal))
                {
                    return false;
                }

                var identity = new ClaimsIdentity(jwtToken.Claims, "jwt");
                context.User = new ClaimsPrincipal(identity);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
