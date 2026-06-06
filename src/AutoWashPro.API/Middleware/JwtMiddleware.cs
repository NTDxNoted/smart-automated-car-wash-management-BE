using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AutoWashPro.API.Middleware
{
  public class JwtMiddleware
  {
    private readonly RequestDelegate _next;
    private readonly string _jwtSecret;

    public JwtMiddleware(RequestDelegate next, IConfiguration configuration)
    {
      _next = next;
      _jwtSecret = configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT secret is not configured.");
    }

    public async Task InvokeAsync(HttpContext context)
    {
      var token = ExtractToken(context);
      if (!string.IsNullOrEmpty(token))
      {
        AttachUserToContext(context, token);
      }

      await _next(context);
    }

    private static string? ExtractToken(HttpContext context)
    {
      var authorization = context.Request.Headers["Authorization"].FirstOrDefault();
      if (string.IsNullOrEmpty(authorization))
        return null;

      var parts = authorization.Split(' ');
      if (parts.Length != 2 || !parts[0].Equals("Bearer", StringComparison.OrdinalIgnoreCase))
        return null;

      return parts[1];
    }

    private void AttachUserToContext(HttpContext context, string token)
    {
      try
      {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSecret);
        tokenHandler.ValidateToken(token, new TokenValidationParameters
        {
          ValidateIssuerSigningKey = true,
          IssuerSigningKey = new SymmetricSecurityKey(key),
          ValidateIssuer = false,
          ValidateAudience = false,
          ClockSkew = TimeSpan.Zero
        }, out var validatedToken);

        if (validatedToken is JwtSecurityToken jwtToken)
        {
          var claims = new ClaimsIdentity(jwtToken.Claims, "jwt");
          context.User = new ClaimsPrincipal(claims);
        }
      }
      catch
      {
        // Ignore invalid token and allow unauthorized access for endpoints that do not require auth.
      }
    }
  }
}
