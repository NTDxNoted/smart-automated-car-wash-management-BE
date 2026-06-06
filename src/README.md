# Car Wash Management System - Backend API

Backend API cho hệ thống quản lý rửa xe tự động.

## 📋 Yêu cầu

- .NET 6.0 SDK trở lên
- SQL Server (hoặc LocalDB)
- Visual Studio 2022 / Visual Studio Code

## 🚀 Setup & Chạy

### 1. Restore Dependencies
```bash
dotnet restore
```

### 2. Update Database
```bash
dotnet ef database update --project Infrastructure -s API
```

Hoặc từ Package Manager Console:
```
Update-Database
```

### 3. Run API
```bash
dotnet run --project API
```

API sẽ chạy tại: `https://localhost:7001`

## 📚 API Endpoints

### Public Endpoints (Không cần Auth)

#### Services
- `GET /api/services` — Danh sách dịch vụ Active
- `GET /api/services/{id}` — Chi tiết dịch vụ

#### Rewards
- `GET /api/rewards` — Danh sách rewards (cần auth Member)

### Admin Endpoints (Cần Auth Admin)

#### Services Management
- `POST /api/admin/services` — Tạo dịch vụ mới
- `PUT /api/admin/services/{id}` — Cập nhật dịch vụ
- `PATCH /api/admin/services/{id}/status` — Toggle Active/Inactive

#### Rewards Management
- `POST /api/admin/rewards` — Tạo reward mới
- `PUT /api/admin/rewards/{id}` — Cập nhật reward
- `PATCH /api/admin/rewards/{id}/toggle` — Toggle IsActive

## 📝 Database Connection

Mặc định project dùng SQL Server LocalDB:

**appsettings.json:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CarWashManagement;Trusted_Connection=true;"
}
```

Để thay đổi, edit `appsettings.json` hoặc set environment variable.

## 🏗️ Project Structure

```
Domain/           — Entities (Service, RewardsCatalog)
Application/      — Interfaces, Services, DTOs
Infrastructure/   — DbContext, Repositories
API/              — Controllers, Startup config
```

## 🛠️ Công cụ & Libraries

- **ASP.NET Core 6.0** — Web Framework
- **Entity Framework Core 6.0** — ORM
- **SQL Server** — Database
- **Swagger** — API Documentation
- **JWT Bearer** — Authentication

## 📖 Swagger Documentation

Khi chạy API, truy cập:
```
https://localhost:7001/swagger
```

## ⚙️ Cấu hình Authentication

Edit `Program.cs` để cấu hình Auth provider:
```csharp
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience = builder.Configuration["Auth:Audience"];
        options.RequireHttpsMetadata = false;
    });
```

## 🐛 Troubleshooting

### Database Connection Error
- Kiểm tra SQL Server LocalDB đã cài đặt: `sqllocaldb i`
- Hoặc chỉnh ConnectionString trong `appsettings.json`

### Migration Error
```bash
dotnet ef migrations add InitialCreate --project Infrastructure -s API
dotnet ef database update --project Infrastructure -s API
```

### Port Already In Use
Edit `Properties/launchSettings.json` và thay đổi port.

---

**Author**: CarWash Team  
**Version**: 1.0.0
