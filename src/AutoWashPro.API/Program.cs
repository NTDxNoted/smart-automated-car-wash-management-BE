using Microsoft.EntityFrameworkCore;
using AutoWash.Infrastructure;
using AutoWash.Infrastructure.Data;
using AutoWash.Application.Interfaces;
using AutoWash.Application.Services;

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
