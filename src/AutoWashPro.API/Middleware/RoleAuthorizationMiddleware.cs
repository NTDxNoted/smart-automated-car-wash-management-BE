using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AutoWashPro.API.Middleware
{
    public class RoleAuthorizationMiddleware
    {
        private readonly RequestDelegate _next;

        public RoleAuthorizationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // CORS preflight requests carry no Authorization header by design;
            // let them pass through so the browser gets a successful preflight response.
            if (HttpMethods.IsOptions(context.Request.Method))
            {
                await _next(context);
                return;
            }

            var path = context.Request.Path.Value ?? "";

            // Intercept admin endpoints except the login endpoint
            if (path.StartsWith("/api/admin", StringComparison.OrdinalIgnoreCase) &&
                !path.StartsWith("/api/admin/auth/login", StringComparison.OrdinalIgnoreCase))
            {
                var user = context.User;
                var isAuthorized = false;

                if (user?.Identity?.IsAuthenticated == true)
                {
                    var roleClaim = user.FindFirst("role")?.Value ?? user.FindFirst(ClaimTypes.Role)?.Value;
                    if (roleClaim != null && roleClaim.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                    {
                        isAuthorized = true;
                    }
                }

                if (!isAuthorized)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    var errorResponse = new
                    {
                        error = "FORBIDDEN",
                        message = "Bạn không có quyền truy cập vào tài nguyên này"
                    };
                    await context.Response.WriteAsJsonAsync(errorResponse);
                    return;
                }
            }

            await _next(context);
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeAdminAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            var isAuthorized = false;

            if (user?.Identity?.IsAuthenticated == true)
            {
                var roleClaim = user.FindFirst("role")?.Value ?? user.FindFirst(ClaimTypes.Role)?.Value;
                if (roleClaim != null && roleClaim.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                {
                    isAuthorized = true;
                }
            }

            if (!isAuthorized)
            {
                context.Result = new JsonResult(new
                {
                    error = "FORBIDDEN",
                    message = "Bạn không có quyền truy cập vào tài nguyên này"
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            return Task.CompletedTask;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeMemberAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            var isAuthorized = false;

            if (user?.Identity?.IsAuthenticated == true)
            {
                var roleClaim = user.FindFirst("role")?.Value ?? user.FindFirst(ClaimTypes.Role)?.Value;
                // Only allow "Member" (case-insensitive)
                if (roleClaim != null && roleClaim.Equals("Member", StringComparison.OrdinalIgnoreCase))
                {
                    isAuthorized = true;
                }
            }

            if (!isAuthorized)
            {
                context.Result = new JsonResult(new
                {
                    error = "FORBIDDEN",
                    message = "Bạn không có quyền truy cập vào tài nguyên này"
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            return Task.CompletedTask;
        }
    }
}
