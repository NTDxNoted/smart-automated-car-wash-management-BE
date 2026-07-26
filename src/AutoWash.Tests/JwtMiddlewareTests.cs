using AutoWash.Domain.Entities;
using AutoWash.Infrastructure.Data;
using AutoWashPro.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AutoWash.Tests.Application.Services
{
  public class JwtMiddlewareTests
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
      var inMemorySettings = new Dictionary<string, string>
            {
                { "Jwt:SecretKey", "AutoWashPro_SecretKey_2025_MustBe32CharsLong!!" }
            };

      return new ConfigurationBuilder()
          .AddInMemoryCollection(inMemorySettings)
          .Build();
    }

    private string CreateToken(int userId, string sessionId, string secret)
    {
      var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("SessionId", sessionId)
            };

      var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret));
      var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
      var token = new JwtSecurityToken(
          claims: claims,
          expires: DateTime.UtcNow.AddMinutes(30),
          signingCredentials: credentials);

      return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task InvokeAsync_WithMismatchedSessionId_ShouldReturnUnauthorized()
    {
      var dbContext = CreateDbContext();
      var configuration = CreateConfiguration();
      var customer = new Customer
      {
        FullName = "Test User",
        Phone = "0901111111",
        Password = "hashed",
        Role = "MEMBER",
        ActiveSessionId = "db-session-id",
        IsLocked = false,
        CreatedAt = DateTime.UtcNow
      };

      dbContext.Customers.Add(customer);
      await dbContext.SaveChangesAsync();

      var nextCalled = false;
      RequestDelegate next = async _ =>
      {
        nextCalled = true;
        await Task.CompletedTask;
      };

      var middleware = new JwtMiddleware(next, configuration, dbContext);
      var context = new DefaultHttpContext();
      context.Request.Headers.Authorization = $"Bearer {CreateToken(customer.CustomerID, "token-session-id", configuration["Jwt:SecretKey"])}";
      context.Response.Body = new MemoryStream();

      await middleware.InvokeAsync(context);

      Assert.False(nextCalled);
      Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
      context.Response.Body.Position = 0;
      var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
      Assert.Contains("SESSION_EXPIRED", body);
    }

    [Fact]
    public async Task InvokeAsync_WithMatchingSessionId_ShouldAttachUserAndContinue()
    {
      var dbContext = CreateDbContext();
      var configuration = CreateConfiguration();
      var customer = new Customer
      {
        FullName = "Test User",
        Phone = "0901111112",
        Password = "hashed",
        Role = "MEMBER",
        ActiveSessionId = "matching-session-id",
        IsLocked = false,
        CreatedAt = DateTime.UtcNow
      };

      dbContext.Customers.Add(customer);
      await dbContext.SaveChangesAsync();

      var nextCalled = false;
      RequestDelegate next = async context =>
      {
        nextCalled = true;
        await Task.CompletedTask;
      };

      var middleware = new JwtMiddleware(next, configuration, dbContext);
      var context = new DefaultHttpContext();
      context.Request.Headers.Authorization = $"Bearer {CreateToken(customer.CustomerID, "matching-session-id", configuration["Jwt:SecretKey"])}";

      await middleware.InvokeAsync(context);

      Assert.True(nextCalled);
      Assert.NotNull(context.User);
      Assert.True(context.User.Identity?.IsAuthenticated);
      Assert.Equal(customer.CustomerID.ToString(), context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }
  }
}
