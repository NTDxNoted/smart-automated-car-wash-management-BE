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


var builder = WebApplication.CreateBuilder(args);

// 1. Thêm dịch vụ Controller
builder.Services.AddControllers();

// 2. Thêm dịch vụ Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructureServices(builder.Configuration);
// Nối IApplicationDbContext tới ApplicationDbContext
builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

// ISSUE-01: Authentication & Authorization
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"];
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
});

builder.Services.AddScoped<IAuthService, AuthService>();

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

builder.Services.AddSingleton<IOtpService, OtpService>();

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

// ISSUE-10: Loyalty Points & Redemption (PointExpiryJob + repository)
builder.Services.AddScoped<PointTransactionRepository>();
builder.Services.AddHostedService<PointExpiryJob>();

builder.Services.AddAuthorization();
var app = builder.Build();

// 3. Kích hoạt giao diện Swagger khi đang code (Development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ISSUE-01: JWT Middleware
app.UseMiddleware<JwtMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
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
