# 🚗 AutoWash Pro
### Smart Automated Car Wash Management System with Advance Booking & Loyalty Program

> SWP391 — FPT University | Summer 2026

---

## 📋 Overview

AutoWash Pro is a smart car wash management system that integrates a multi-tier loyalty program with advance booking, helping businesses boost customer retention and streamline daily operations.

**Key Features:**
- Advance booking with tier-based priority queue
- Multi-tier loyalty program (Member → Silver → Gold → Platinum)
- Points earning, expiry & redemption engine
- Offline payment management (Cash / Transfer)
- Admin dashboard with RFM analytics

---

## 🛠 Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | React (Vite) + Tailwind CSS + Axios |
| Backend | C# .NET 8 Web API + Entity Framework Core |
| Database | PostgreSQL (Supabase) |
| API Docs | Swagger UI |
| Auth | JWT Bearer Token |

---

## 📁 Project Structure

```
AutoWashPro/
├── src/
│   ├── AutoWash.sln
│   ├── AutoWash.API/           # Controllers, Middleware, Program.cs
│   ├── AutoWash.Application/   # Services, DTOs, Interfaces, Validators
│   ├── AutoWash.Domain/        # Entities, Enums
│   └── AutoWash.Infrastructure/# DbContext, Repositories, Jobs
├── docs/
│   ├── CONTEXT.md
│   ├── issues.md
│   ├── functional_requirements.md
│   └── CONTRIBUTING.md
└── script/
    └── autowash_supabase.sql
```

---

## 🚀 Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL](https://www.postgresql.org/) or Supabase account
- [Node.js](https://nodejs.org/) (for frontend)

### Backend Setup

```bash
# 1. Clone repo
git clone https://github.com/NTDxNoted/smart-automated-car-wash-management-BE.git
cd smart-automated-car-wash-management-BE

# 2. Go to src
cd src

# 3. Restore packages
dotnet restore

# 4. Update connection string in appsettings.json
# AutoWash.API/appsettings.json → "DefaultConnection": "your-supabase-url"

# 5. Run API
cd AutoWash.API
dotnet run
```

### Access Swagger UI
```
https://localhost:5001/swagger
```

---

## 📖 Docs

| File | Mô tả |
|------|-------|
| [CONTEXT.md](docs/CONTEXT.md) | 66 Business Rules |
| [functional_requirements.md](docs/functional_requirements.md) | 38 Functional Requirements |
| [issues.md](docs/issues.md) | 12 Issues chi tiết |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Git workflow & conventions |

---

## 👥 Team

| Role | Trách nhiệm |
|------|------------|
| Leader | System design, code review, merge PR |
| Backend Dev | .NET API, business logic |

---

## 📌 Issue Tracking

Xem toàn bộ issues tại: [GitHub Issues](https://github.com/NTDxNoted/smart-automated-car-wash-management-BE/issues)

| Priority | Issues |
|----------|--------|
| 🔴 P0 | ISSUE-01, 02, 04, 06, 07, 08, 09 |
| 🟠 P1 | ISSUE-03, 05, 10, 11 |
| 🟡 P2 | ISSUE-12 |
