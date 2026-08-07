using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AutoWash.Infrastructure;
using AutoWash.Infrastructure.Data;
using AutoWash.Application.Interfaces;
using AutoWash.Application.Services;
using AutoWash.Infrastructure.Repositories;
using AutoWash.Infrastructure.Jobs;
using AutoWashPro.API.Middleware;
using AutoWashPro.API.Hubs;


AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// 1. Thêm dịch vụ Controller
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
    });
builder.Services.Configure<AutoWash.Application.DTOs.BookingSettings>(builder.Configuration.GetSection("BookingSettings"));

// CORS — cho phép frontend dev server gọi API
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 2. Thêm dịch vụ Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructureServices(builder.Configuration);
// Nối IApplicationDbContext tới ApplicationDbContext
builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

// ISSUE-01: Authentication & Authorization
var jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? builder.Configuration["Jwt:SecretKey"];
if (string.IsNullOrWhiteSpace(jwtSecretKey))
{
    jwtSecretKey = "AutoWashPro_SecretKey_2025_MustBe32CharsLong!!";
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecretKey)),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // SignalR: trình duyệt không set được header Authorization tùy ý lúc bắt tay WebSocket,
    // nên client gửi token qua query string "access_token" thay (xem signalrService.js bên FE).
    // Chỉ áp dụng cho đúng path của hub, các request API bình thường vẫn dùng header như cũ.
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// SignalR: đẩy thông báo real-time cho Admin, thay cho polling 5 giây ở FE.
builder.Services.AddSignalR();
builder.Services.AddScoped<IAdminNotifier, SignalRAdminNotifier>();
// BookingHub: đẩy cập nhật số chỗ trống theo thời gian thực cho trang đặt lịch (public, cả Guest lẫn Member).
builder.Services.AddScoped<IBookingHubNotifier, SignalRBookingHubNotifier>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminAuthService, AdminAuthService>();

// Nối IBookingService tới BookingService
builder.Services.AddScoped<IBookingsService, BookingService>();

// ISSUE-04: Service & Rewards Catalog
builder.Services.AddScoped<IServiceService, ServiceService>();
builder.Services.AddScoped<IRewardService, RewardService>();
builder.Services.AddScoped<ServiceRepository>();
builder.Services.AddScoped<RewardRepository>();

// ISSUE-02: Member Profile & Vehicle Management
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<VehicleRepository>();
builder.Services.AddScoped<IAdminCustomerService, AdminCustomerService>();
//.Services.AddScoped<AutoWashPro.API.Filters.BookingFinancialProtectionFilter>();

// ISSUE-11: Tier Upgrade/Downgrade
builder.Services.AddScoped<ITierService, TierService>();
builder.Services.AddScoped<TierRepository>();
builder.Services.AddHostedService<TierDowngradeJob>();

// ISSUE-08: Admin Booking Workflow
builder.Services.AddScoped<IAdminBookingService, AdminBookingService>();
builder.Services.AddHostedService<AutoNoShowJob>();

// ISSUE-09: Offline Payment Processing
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPointService, PointService>();
builder.Services.AddScoped<TransactionRepository>();

// ISSUE-12: Admin Reports & RFM Dashboard
builder.Services.AddScoped<IReportService, ReportService>();

// ISSUE-10: Loyalty Points & Redemption (PointExpiryJob + repository)
builder.Services.AddScoped<PointTransactionRepository>();
builder.Services.AddHostedService<PointExpiryJob>();

// ISSUE-05: Promotion Management
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddAuthorization();
var app = builder.Build();

// 3. Kích hoạt giao diện Swagger khi đang code (Development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();
app.UseCors("FrontendDev");

// ISSUE-01: JWT Middleware
app.UseMiddleware<JwtMiddleware>();
app.UseMiddleware<RoleAuthorizationMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<AdminNotificationHub>("/hubs/admin-notifications");
app.MapHub<BookingHub>("/hubs/booking");
// Nút test kết nối Database
app.MapGet("/api/test-db", async (ApplicationDbContext dbContext) =>
{
    try
    {
        bool canConnect = await dbContext.Database.CanConnectAsync();
        if (canConnect)
        {
            return Results.Ok(" Kết nối PostgreSQL thành công rực rỡ!");
        }
        return Results.Problem("Kết nối thất bại nhưng không rõ lỗi.");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Lỗi rồi: {ex.Message}");
    }
});

app.Run();
