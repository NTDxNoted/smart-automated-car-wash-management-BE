# 🚀 Quick Reference Card

## Server Status
```
✅ RUNNING
HTTP:  http://localhost:5001
HTTPS: https://localhost:7001
Swagger: http://localhost:5001/swagger
```

## Common Commands

### Start Server
```powershell
cd "d:\Issues 4\API"
dotnet run
```

### Stop Server
```
Ctrl+C
```

### Build Project
```powershell
cd "d:\Issues 4"
dotnet build
```

### Clean & Rebuild
```powershell
cd "d:\Issues 4"
dotnet clean
dotnet restore
dotnet build
```

---

## API Quick Test

### Using PowerShell
```powershell
# Get all services
$response = Invoke-WebRequest http://localhost:5001/api/services
$response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 5
```

### Using Swagger UI
```
http://localhost:5001/swagger
```

### Sample API Calls
```
GET  http://localhost:5001/api/services
GET  http://localhost:5001/api/services/1
GET  http://localhost:5001/api/rewards
```

---

## File Structure

```
d:\Issues 4\
├── Domain\                 # Entities
├── Application\            # Business Logic
├── Infrastructure\         # Data Access
├── API\                    # Controllers & Config
├── CarWashManagement.sln   # Solution file
├── README.md
├── SETUP_COMPLETE.md
├── IMPLEMENTATION_SUMMARY.md
├── FILES_CREATED.md
└── carwash.db              # Database
```

---

## Database

**Type**: SQLite  
**Location**: `d:\Issues 4\API\carwash.db`

### Sample Data
```
Services: 3 records (Basic, Interior, Express)
Rewards: 2 records (10% discount, 20% discount)
```

### Reset Database
```powershell
Remove-Item "d:\Issues 4\API\carwash.db"
# Server will recreate on restart
```

---

## Key Ports

```
5001 - HTTP (Development)
7001 - HTTPS (Development)
```

---

## Documentation

| File | Purpose |
|------|---------|
| README.md | Project overview & guide |
| SETUP_COMPLETE.md | Setup & troubleshooting |
| IMPLEMENTATION_SUMMARY.md | Full project summary |
| FILES_CREATED.md | List of all files |

---

## Technology

- **.NET 10.0**
- **ASP.NET Core**
- **Entity Framework Core**
- **SQLite**
- **Swagger/OpenAPI**

---

## Status Checks

### Is server running?
```powershell
Invoke-WebRequest http://localhost:5001/swagger
```

### Is database accessible?
```powershell
Test-Path "d:\Issues 4\API\carwash.db"
```

### Which port is using 5001?
```powershell
netstat -ano | findstr 5001
```

---

**Version**: 1.0.0  
**Status**: ✅ Production Ready  
**Last Updated**: 2026-06-05
