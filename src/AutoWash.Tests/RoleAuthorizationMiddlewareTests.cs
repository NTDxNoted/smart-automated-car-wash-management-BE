using System.Security.Claims;
using System.Text.Json;
using AutoWashPro.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Xunit;

namespace AutoWash.Tests.Application.Services
{
  public class RoleAuthorizationMiddlewareTests
  {
    private static DefaultHttpContext CreateContext(string path, ClaimsPrincipal? user = null)
    {
      var context = new DefaultHttpContext();
      context.Request.Path = path;
      context.Request.Method = "GET";
      context.Response.Body = new MemoryStream();
      if (user != null) context.User = user;
      return context;
    }

    private static ClaimsPrincipal AuthenticatedUser(string role)
    {
      var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "jwt");
      return new ClaimsPrincipal(identity);
    }

    private static async Task<(int StatusCode, string? Error)> ReadErrorAsync(DefaultHttpContext context)
    {
      context.Response.Body.Seek(0, SeekOrigin.Begin);
      using var reader = new StreamReader(context.Response.Body);
      var body = await reader.ReadToEndAsync();
      if (string.IsNullOrEmpty(body)) return (context.Response.StatusCode, null);

      using var doc = JsonDocument.Parse(body);
      var error = doc.RootElement.TryGetProperty("error", out var e) ? e.GetString() : null;
      return (context.Response.StatusCode, error);
    }

    [Fact]
    public async Task InvokeAsync_AdminPathWithoutToken_ShouldReturn401Unauthorized()
    {
      var context = CreateContext("/api/admin/customers"); // context.User mặc định là empty ClaimsPrincipal, chưa authenticated
      var middleware = new RoleAuthorizationMiddleware(_ => Task.CompletedTask);

      await middleware.InvokeAsync(context);

      var (statusCode, error) = await ReadErrorAsync(context);
      Assert.Equal(StatusCodes.Status401Unauthorized, statusCode);
      Assert.Equal("UNAUTHORIZED", error);
    }

    [Fact]
    public async Task InvokeAsync_AdminPathWithMemberRole_ShouldReturn403Forbidden()
    {
      var context = CreateContext("/api/admin/customers", AuthenticatedUser("Member"));
      var middleware = new RoleAuthorizationMiddleware(_ => Task.CompletedTask);

      await middleware.InvokeAsync(context);

      var (statusCode, error) = await ReadErrorAsync(context);
      Assert.Equal(StatusCodes.Status403Forbidden, statusCode);
      Assert.Equal("FORBIDDEN", error);
    }

    [Fact]
    public async Task InvokeAsync_AdminPathWithAdminRole_ShouldPassThrough()
    {
      var context = CreateContext("/api/admin/customers", AuthenticatedUser("Admin"));
      var nextCalled = false;
      var middleware = new RoleAuthorizationMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

      await middleware.InvokeAsync(context);

      Assert.True(nextCalled);
      Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode); // DefaultHttpContext mặc định 200, middleware không đổi
    }

    [Fact]
    public async Task InvokeAsync_AdminLoginPath_ShouldPassThroughWithoutAuth()
    {
      var context = CreateContext("/api/admin/auth/login");
      var nextCalled = false;
      var middleware = new RoleAuthorizationMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

      await middleware.InvokeAsync(context);

      Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_NonAdminPath_ShouldPassThroughRegardlessOfAuth()
    {
      var context = CreateContext("/api/bookings");
      var nextCalled = false;
      var middleware = new RoleAuthorizationMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

      await middleware.InvokeAsync(context);

      Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_OptionsPreflight_ShouldPassThroughEvenForAdminPath()
    {
      var context = CreateContext("/api/admin/customers");
      context.Request.Method = "OPTIONS";
      var nextCalled = false;
      var middleware = new RoleAuthorizationMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

      await middleware.InvokeAsync(context);

      Assert.True(nextCalled);
    }

    private static AuthorizationFilterContext CreateFilterContext(ClaimsPrincipal? user)
    {
      var httpContext = new DefaultHttpContext();
      if (user != null) httpContext.User = user;

      var actionContext = new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor();
      var routeData = new Microsoft.AspNetCore.Routing.RouteData();
      var context = new Microsoft.AspNetCore.Mvc.ActionContext(httpContext, routeData, actionContext);
      return new AuthorizationFilterContext(context, new List<IFilterMetadata>());
    }

    [Fact]
    public async Task AuthorizeAdminAttribute_WithoutAuth_ShouldSet401()
    {
      var filterContext = CreateFilterContext(user: null);
      var attribute = new AuthorizeAdminAttribute();

      await attribute.OnAuthorizationAsync(filterContext);

      var result = Assert.IsType<JsonResult>(filterContext.Result);
      Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task AuthorizeAdminAttribute_WithMemberRole_ShouldSet403()
    {
      var filterContext = CreateFilterContext(AuthenticatedUser("Member"));
      var attribute = new AuthorizeAdminAttribute();

      await attribute.OnAuthorizationAsync(filterContext);

      var result = Assert.IsType<JsonResult>(filterContext.Result);
      Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task AuthorizeAdminAttribute_WithAdminRole_ShouldNotSetResult()
    {
      var filterContext = CreateFilterContext(AuthenticatedUser("Admin"));
      var attribute = new AuthorizeAdminAttribute();

      await attribute.OnAuthorizationAsync(filterContext);

      Assert.Null(filterContext.Result);
    }

    [Fact]
    public async Task AuthorizeMemberAttribute_WithoutAuth_ShouldSet401()
    {
      var filterContext = CreateFilterContext(user: null);
      var attribute = new AuthorizeMemberAttribute();

      await attribute.OnAuthorizationAsync(filterContext);

      var result = Assert.IsType<JsonResult>(filterContext.Result);
      Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task AuthorizeMemberAttribute_WithAdminRole_ShouldSet403()
    {
      var filterContext = CreateFilterContext(AuthenticatedUser("Admin"));
      var attribute = new AuthorizeMemberAttribute();

      await attribute.OnAuthorizationAsync(filterContext);

      var result = Assert.IsType<JsonResult>(filterContext.Result);
      Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task AuthorizeMemberAttribute_WithMemberRole_ShouldNotSetResult()
    {
      var filterContext = CreateFilterContext(AuthenticatedUser("Member"));
      var attribute = new AuthorizeMemberAttribute();

      await attribute.OnAuthorizationAsync(filterContext);

      Assert.Null(filterContext.Result);
    }
  }
}
