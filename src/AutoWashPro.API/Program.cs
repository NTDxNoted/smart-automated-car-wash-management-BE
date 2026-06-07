using Microsoft.EntityFrameworkCore;
using AutoWash.Infrastructure;
using AutoWash.Infrastructure.Data;
using AutoWash.Application.Interfaces;
using AutoWash.Application.Services;
using AutoWash.Infrastructure.Repositories;


var builder = WebApplication.CreateBuilder(args);

// 1. Thêm dịch vụ Controller
builder.Services.AddControllers();

// 2. Thêm dịch vụ Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructureServices(builder.Configuration);
// Nối IApplicationDbContext tới ApplicationDbContext
builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

// Nối IBookingService tới BookingService
builder.Services.AddScoped<IBookingsService, BookingService>();


//builder.Services.AddScoped<IAuthService, AuthService>();
//builder.Services.AddScoped<CustomerRepository>();

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
builder.Services.AddScoped<AutoWashPro.API.Filters.BookingFinancialProtectionFilter>();

builder.Services.AddSingleton<IOtpService, OtpService>();

builder.Services.AddAuthorization();
var app = builder.Build();

// 3. Kích hoạt giao diện Swagger khi đang code (Development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


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
