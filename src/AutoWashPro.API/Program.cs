using Microsoft.EntityFrameworkCore;
using AutoWash.Infrastructure;
using AutoWash.Infrastructure.Data;
using AutoWash.Infrastructure.Repositories;
using AutoWash.Application.Interfaces;
using AutoWash.Application.Services;
using AutoWashPro.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// 1. Thêm dịch vụ Controller
builder.Services.AddControllers();

// 2. Thêm dịch vụ Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
builder.Services.AddScoped<IBookingsService, BookingService>();

// ISSUE-02: Member Profile & Vehicle Management
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<VehicleRepository>();
builder.Services.AddSingleton<IOtpService, OtpService>();

builder.Services.AddAuthorization();
var app = builder.Build();

// 3. Kích hoạt giao diện Swagger khi đang code (Development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<JwtMiddleware>();
app.UseAuthorization();
app.MapControllers();
// Nút test kết nối Database
app.MapGet("/api/test-db", async (ApplicationDbContext dbContext) =>
{
    try
    {
        // Hàm CanConnectAsync() sẽ thử ping vào database
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
