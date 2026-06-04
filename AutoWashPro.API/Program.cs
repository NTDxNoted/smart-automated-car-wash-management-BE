var builder = WebApplication.CreateBuilder(args);

// 1. Thêm dịch vụ Controller
builder.Services.AddControllers();

// 2. Thêm dịch vụ Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 3. Kích hoạt giao diện Swagger khi đang code (Development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();