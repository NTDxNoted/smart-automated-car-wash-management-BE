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

                // 401: chưa đăng nhập / token thiếu, sai hoặc hết hạn — JwtMiddleware không gán được User.
                // Khác với 403 (đã đăng nhập nhưng không đủ quyền) để FE phân biệt được lúc nào cần
                // yêu cầu đăng nhập lại và lúc nào chỉ đơn giản là không có quyền.
                if (user?.Identity?.IsAuthenticated != true)
                {
                    await WriteErrorAsync(context, StatusCodes.Status401Unauthorized, "UNAUTHORIZED",
                        "Bạn cần đăng nhập để truy cập tài nguyên này.");
                    return;
                }

                var roleClaim = user.FindFirst("role")?.Value
                    ?? user.FindFirst(ClaimTypes.Role)?.Value
                    ?? user.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
                var isAdmin = roleClaim != null && roleClaim.Equals("Admin", StringComparison.OrdinalIgnoreCase);

                if (!isAdmin)
                {
                    await WriteErrorAsync(context, StatusCodes.Status403Forbidden, "FORBIDDEN",
                        "Bạn không có quyền truy cập vào tài nguyên này");
                    return;
                }
            }

            await _next(context);
        }

        private static Task WriteErrorAsync(HttpContext context, int statusCode, string error, string message)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsJsonAsync(new { error, message }, cancellationToken: context.RequestAborted);
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeAdminAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (user?.Identity?.IsAuthenticated != true)
            {
                context.Result = Unauthorized();
                return Task.CompletedTask;
            }

            var roleClaim = user.FindFirst("role")?.Value ?? user.FindFirst(ClaimTypes.Role)?.Value;
            var isAdmin = roleClaim != null && roleClaim.Equals("Admin", StringComparison.OrdinalIgnoreCase);

            if (!isAdmin)
            {
                context.Result = Forbidden();
            }

            return Task.CompletedTask;
        }

        internal static JsonResult Unauthorized() => new JsonResult(new
        {
            error = "UNAUTHORIZED",
            message = "Bạn cần đăng nhập để truy cập tài nguyên này."
        })
        { StatusCode = StatusCodes.Status401Unauthorized };

        internal static JsonResult Forbidden() => new JsonResult(new
        {
            error = "FORBIDDEN",
            message = "Bạn không có quyền truy cập vào tài nguyên này"
        })
        { StatusCode = StatusCodes.Status403Forbidden };
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeMemberAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (user?.Identity?.IsAuthenticated != true)
            {
                context.Result = AuthorizeAdminAttribute.Unauthorized();
                return Task.CompletedTask;
            }

            var roleClaim = user.FindFirst("role")?.Value ?? user.FindFirst(ClaimTypes.Role)?.Value;
            // Only allow "Member" (case-insensitive)
            var isMember = roleClaim != null && roleClaim.Equals("Member", StringComparison.OrdinalIgnoreCase);

            if (!isMember)
            {
                context.Result = AuthorizeAdminAttribute.Forbidden();
            }

            return Task.CompletedTask;
        }
    }
}
