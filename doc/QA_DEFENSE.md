# Q&A bảo vệ đồ án — Câu hỏi ngoài lề

> Tổng hợp trả lời cho các câu hỏi phản biện thường gặp, dựa trên đọc trực tiếp source code backend (AutoWashPro.API / AutoWash.Application / AutoWash.Infrastructure / AutoWash.Domain). Mỗi câu có trích dẫn file:line để tra lại khi cần.

---

## 1. Hình ảnh lưu ở đâu?

**Chưa triển khai.** Đã grep toàn bộ `.cs` cho `Image|Upload|Cloudinary|wwwroot|Blob|ImageUrl|AvatarUrl` — không có kết quả. Các entity (`Customer`, `Vehicle`, `Service`...) không có cột lưu ảnh.

Trả lời hội đồng: backend hiện tại **không có tính năng lưu/upload ảnh**. Nếu bị hỏi ép, nói rõ đây là phần chưa nằm trong scope các issue đã làm, không bịa ra Cloudinary/S3/wwwroot nếu không có trong code.

---

## 2. Cấu hình phải nằm ở server, không nằm ở client

Nhận định đúng, và kiến trúc hiện tại tuân theo nguyên tắc đó: các cấu hình nghiệp vụ (VD `BookingSettings.MaxParallelSlots`, `PriorityBufferSlots`) được đọc từ `appsettings.json` phía server qua:

```csharp
builder.Services.Configure<BookingSettings>(builder.Configuration.GetSection("BookingSettings"));
// src/AutoWashPro.API/Program.cs:21
```

Client chỉ gửi request (VD đặt lịch), server validate + tính toán + ghi DB qua EF Core. Không có cấu hình nghiệp vụ nào nằm ở phía client trong repo backend.

---

## 3. Password phải hash

Đúng, dùng **BCrypt** (`BCrypt.Net-Core`, v1.6.0):

- Đăng ký: `BCrypt.Net.BCrypt.HashPassword(request.Password)` — `AuthService.cs:57`
- Đăng nhập: `BCrypt.Net.BCrypt.Verify(request.Password, customer.Password)` — `AuthService.cs:124`, tương tự `AdminAuthService.cs:56`

Không lưu plaintext password vào DB ở bất kỳ đâu.

---

## 4. Cơ chế sinh JWT (khác gì đăng nhập Gmail)

Đây là **JWT tự phát hành (self-issued)**, không phải OAuth như Google login. Flow trong `AuthService.cs:160-192` (`GenerateJwtToken`):

1. Xác thực phone + password bằng BCrypt.
2. Sinh `ActiveSessionId` (GUID) mới, ghi đè vào DB — cơ chế single-session: đăng nhập ở máy mới sẽ tự vô hiệu hoá JWT cũ đang cầm `SessionId` khác.
3. Tạo `SymmetricSecurityKey` từ `Jwt:SecretKey`, ký bằng thuật toán đối xứng `HmacSha256` (cùng 1 key để ký và verify).
4. Claims đính kèm: `NameIdentifier` (CustomerID), `Name`, `phone`, `tier`, `Role`, `SessionId`.
5. `new JwtSecurityTokenHandler().WriteToken(token)` → trả token cho client.

Khác biệt với login Gmail: đây là JWT tự cấp phát nội bộ (không qua bên thứ ba); Google dùng chuẩn OAuth2/OpenID Connect với authorization server riêng và ký bằng khoá bất đối xứng.

---

## 5. API path cấu hình ở đâu

Route khai báo bằng attribute ngay trong Controller, không có file config route riêng:

```csharp
[ApiController]
[Route("api/auth")]        // AuthController.cs:13
[HttpPost("register")]     // -> POST /api/auth/register
```

- Base URL/port (dev): `Properties/launchSettings.json` → `https://localhost:59152;http://localhost:59153`
- Khi deploy container: `ASPNETCORE_URLS=http://+:5000` set trong `Dockerfile:27`

---

## 6. JWT cấu hình trong header ở đâu

Client gửi token qua header `Authorization: Bearer <token>`. Server đọc ở 2 nơi:

- `Middleware/JwtMiddleware.cs:32` — middleware tự viết:
  ```csharp
  var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
  ```
  dùng để kiểm tra `ActiveSessionId` (chặn đăng nhập nhiều thiết bị cùng lúc).
- `Program.cs:54-82` — `AddJwtBearer(...)`: cấu hình chuẩn ASP.NET Authentication, tự đọc header `Authorization`.
- Ngoại lệ SignalR: WebSocket không set được header lúc handshake, nên với path `/hubs/*` token được gửi qua query string `?access_token=...`, xử lý riêng ở `OnMessageReceived` (`Program.cs:71-79`).

---

## 7. Backend lưu DB thế nào — layer nào gọi layer nào — dùng gì để connect DB

**Clean Architecture 4 lớp:**

```
AutoWashPro.API        (Controllers, Middleware)
    -> AutoWash.Application (Services, Interfaces, DTOs)
        -> AutoWash.Infrastructure (Repositories, DbContext)
            -> AutoWash.Domain (Entities — không phụ thuộc lớp nào khác)
```

- Controller gọi Service qua interface (Dependency Injection) — VD `IAuthService` inject vào `AuthController`.
- Service gọi `IApplicationDbContext` (interface định nghĩa ở Application, implement ở Infrastructure). Nối 2 bên ở `Program.cs:40`:
  ```csharp
  builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
  ```
- **Kết nối DB dùng EF Core (ORM) + provider Npgsql cho PostgreSQL** — `Infrastructure/DependencyInjection.cs:12-13`:
  ```csharp
  services.AddDbContext<ApplicationDbContext>(options =>
      options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
  ```
  Tương đương JDBC bên Java, nhưng đây là ADO.NET provider + EF Core làm ORM (tương đương Hibernate/JPA).
- Bảng thật lưu trên **PostgreSQL do Supabase host** (thấy trong connection string). Mapping bảng ↔ entity khai báo trong `OnModelCreating` của `Infrastructure/Data/ApplicationDbContext.cs` — VD `entity.ToTable("customer")`, `entity.Property(...).HasColumnName("customerid")`.
- Endpoint test kết nối: `GET /api/test-db` — `Program.cs:155-170`.

---

## 8 & 9. Ai deploy — connect được không — Vercel là FE hay BE — backend deploy ở đâu

Grep toàn repo cho `vercel|render|railway|azurewebsites|fly.io` — **không có config deploy nào trong repo backend này** (không có `vercel.json`, không có step deploy trong CI).

- **Backend chưa có auto-deploy config trong repo** — chỉ có `Dockerfile` để build container thủ công, và workflow `.github/workflows/sonar.yml` chỉ chạy SonarCloud scan (code quality), không deploy.
- **Vercel gần như chắc chắn không phải nơi deploy backend .NET này** — Vercel tối ưu cho Next.js/frontend serverless, không phù hợp chạy native ASP.NET Core kiểu long-running này. Nếu team có link Vercel, đó là **frontend**.
- Database (Postgres) host trên **Supabase**: `aws-1-ap-northeast-1.pooler.supabase.com` (xem `appsettings.json`).
- Ai deploy backend, deploy ở đâu: **không có evidence trong code** — cần hỏi lại leader/team; có thể hiện tại chỉ chạy local (`dotnet run` / Docker) hoặc leader tự deploy tay lên VPS/Render mà không commit config vào repo.

### ⚠️ Rủi ro bảo mật cần nêu trong báo cáo

`appsettings.json` đang **commit thẳng password DB thật** (`Password=SWP391_SE1924`) và JWT secret key vào Git. Đây là lỗi bảo mật thực sự:
- JWT key đã có fallback đọc từ biến môi trường `JWT_SECRET_KEY` (`Program.cs:43`), nhưng **connection string DB thì chưa** — nên đưa vào phần "hạn chế/rủi ro" khi bảo vệ, và về lâu dài nên chuyển sang User Secrets / biến môi trường, không commit secret vào repo.

---

## 10. Cơ chế làm việc trên GitHub

Theo `CONTRIBUTING.md`:

- **Branch**: `feature/ISSUE-{số}-{mô-tả}` hoặc `fix/ISSUE-{số}-{mô-tả}`, mỗi issue 1 branch riêng, không code nhiều issue trên cùng 1 branch.
- **Commit**: format `[ISSUE-{số}] {Add/Update/Fix/Remove/Refactor} {mô tả ngắn}`.
- **Workflow**: pull `main` mới nhất → tạo branch mới → code đúng scope issue → commit nhỏ thường xuyên → push branch → tạo PR (title `[ISSUE-XX] Tên issue`, description có checklist đã làm) → **Leader review & approve** → merge. Không tự merge PR của mình, không merge khi còn conflict chưa resolve.
- **Folder ownership**: mỗi issue gắn với 1 bộ file/folder cụ thể (bảng chi tiết trong `CONTRIBUTING.md` mục 6), tránh đụng code của issue khác.
- **CI**: SonarCloud scan tự động chạy khi push/PR vào `main` (`.github/workflows/sonar.yml`).

---

## 11. Chụp lại quá trình làm việc với AI → PDF

Đây không phải câu hỏi về code — là việc cần tự làm để chứng minh với hội đồng: chụp màn hình các đoạn hội thoại với AI (Claude Code, ChatGPT, v.v.) trong quá trình phát triển, ghép vào 1 file PDF nộp kèm báo cáo làm minh chứng có sử dụng AI hỗ trợ đúng quy định.

---

## 12. Công nghệ & thư viện sử dụng (Technology Stack)

**Frontend** (`demo-fe-car-wash`, repo riêng — không nằm trong repo backend này nên chỉ liệt kê theo thông tin nhóm cung cấp, chưa tự grep verify):
- Framework: React 18 + Vite.
- Styling: Vanilla CSS + TailwindCSS.
- HTTP Client: Axios (interceptor đính JWT Bearer, bắt lỗi 401/403).
- Realtime: `@microsoft/signalr` — client nhận thông báo từ 2 hub `AdminNotificationHub`/`BookingHub` bên BE.
- Biểu đồ: Recharts / Chart.js.
- Notification: react-toastify.

**Backend** (`smart-automated-car-wash-management-BE`, đã verify bằng code):
- Framework: ASP.NET Core Web API (.NET 8).
- ORM: Entity Framework Core 8 + Npgsql Provider — connect **Supabase PostgreSQL** (mục 7).
- Bảo mật: JWT tự phát hành + `Middleware/JwtMiddleware.cs` tự viết, xác thực Single Active Session (mục 4 & 6).
- Background Job: `IHostedService`/`BackgroundService` — `AutoNoShowJob.cs` quét mỗi phút, tự chuyển booking `Pending` quá hạn **15 phút** chưa check-in sang `NoShow`; nếu khách có **≥ 3 lần no-show trong 30 ngày gần nhất** thì `IsLocked = true` và `SuspendedUntil = now + 15 ngày` (khóa có thời hạn, không phải vĩnh viễn — xem chi tiết code trích trong lịch sử trao đổi).
- Code Quality: SonarCloud + GitHub Actions — `.github/workflows/sonar.yml` scan tự động khi push/PR vào `main` (mục 10).

**Database & triển khai — cần nói cẩn thận, dễ bị hỏi vặn:**
- Database: Supabase Cloud PostgreSQL — đúng, có trong connection string.
- Deploy: repo chỉ có `Dockerfile` để build container **thủ công**; **không có config auto-deploy/CI-CD nào trong repo** (không `vercel.json`, workflow hiện tại chỉ chạy SonarCloud scan, không deploy). Nếu hội đồng hỏi "ai deploy, deploy ở đâu" → trả lời trung thực là chưa có evidence trong code, tránh khẳng định chắc "Deploy: Docker + Supabase" như một pipeline hoàn chỉnh đã chạy tự động (xem mục 8 & 9 và phần rủi ro bảo mật bên dưới nó).

---

## 13. Phạm vi dự án (Scope)

**Có làm (In Scope):**
- Đặt lịch rửa xe trực tuyến, khống chế đặt trước ≥ 60 phút (BR-29).
- Khóa đăng nhập đơn — Single Session Lock, chặn 1 tài khoản đăng nhập 2 máy cùng lúc (mục 4 & 6). ⚠️ Lưu ý: tính năng này **có thật trong code** (`ActiveSessionId` + `JwtMiddleware.cs`) nhưng **không có mã BR chính thức trong `CONTEXT.md`** — tài liệu đó chỉ có BR-01 → BR-66 ("THE 66 RULES SOURCE"). Nếu trước đây đã lỡ gọi là "BR-67" thì đó là số tự đặt, không tra được trong `CONTEXT.md` — không nên khẳng định số này trước hội đồng.
- Loyalty & tích điểm nâng hạng real-time (Member → Silver → Gold → Platinum).
- Đổi điểm thưởng trừ trực tiếp hóa đơn, tối đa 50% giá trị đơn (BR-60).
- Chặn thanh toán trùng (Double Payment 409 Conflict) & tự động phạt no-show (`AutoNoShowJob`, xem mục 12).
- Dashboard & báo cáo RFM cho Admin (xem `ADMIN_ISSUE_FLOWS.md` FE-ISSUE-11).

**Không làm (Out of Scope):**
- Thanh toán online (VNPay/Momo) — chỉ có thanh toán tại chỗ (tiền mặt/QR chuyển khoản tại trạm).
- Hoàn tiền tự động qua ngân hàng.

> Lưu ý: các mã BR (BR-29, BR-60) đã đối chiếu khớp `doc/CONTEXT.md`. Riêng "BR-67" (Single Session Lock) **không tồn tại** trong `CONTEXT.md` — xem cảnh báo ở trên.

---

## 14. Bản chất 3 luồng nghiệp vụ (Workflows)

**Workflow 1 — Master Data Setup:** Admin đăng nhập → thêm/sửa danh mục dịch vụ → cấu hình ngưỡng chi tiêu nâng hạng Tier → tạo mã khuyến mãi Promo Code. (Chi tiết code-level: `ADMIN_ISSUE_FLOWS.md` FE-ISSUE-10.)

**Workflow 2 — Booking Lifecycle:** xem chi tiết 6 bước kỹ thuật ở mục 16 bên dưới (Client đặt lịch → JwtMiddleware → BookingsController → BookingService → DB → SignalR notify Admin).

**Workflow 3 — Dashboard & Reports:** Màn hình thống kê doanh thu, biểu đồ khung giờ cao điểm (Occupancy Rate) và phân tích phân khúc khách hàng RFM. (Chi tiết code-level: `ADMIN_ISSUE_FLOWS.md` FE-ISSUE-11/14/15/16.)

---

## 15. Chi tiết Business Rules: Đặt lịch, Tính tiền, Tích điểm, Nâng hạng

> Đã đối chiếu từng con số với `doc/CONTEXT.md` — tất cả các mã BR dưới đây đều tồn tại đúng như liệt kê, không có mã nào bịa.

**Đặt lịch & khung giờ:**
- BR-29: đặt lịch tối thiểu trước giờ hẹn **60 phút**.
- BR-15→BR-18: hạn đặt trước theo hạng — Guest/Member: 7 ngày, Silver: 10 ngày, Gold: 12 ngày, Platinum: 14 ngày.
- BR-25/26/27: Guest ≤ 1 đơn Pending, Member ≤ 3 đơn Pending, tối đa 2 đơn "chưa hoàn thành" trong ngày.
- BR-28: cùng 1 biển số không được có 2 lịch cách nhau < 120 phút.

**Tính tiền:**
- BR-39: `FinalAmount = BaseAmount − TierDiscount − RewardDiscount − PromotionDiscount`.
- BR-23: giảm giá theo Tier tự động áp dụng, khách không cần chọn tay.
- BR-60: đổi điểm thưởng giảm tối đa **50%** giá trị hóa đơn gốc.

**Tích điểm & nâng hạng:**
- Cộng điểm: chỉ khi booking chuyển `Completed`, công thức `Floor(FinalAmount / 10000)` — **đã verify đúng trong code** `BookingService.cs:422`: `PointsEarned = (int)Math.Max(0, Math.Floor(finalAmount / 10000m))`. Đơn `Pending` chưa cộng điểm, đúng như lưu ý chống hack điểm.
- BR-21 / BR-21.1: nâng hạng real-time ngay khi `TotalSpending` vượt mốc — Silver ≥ 500k, Gold ≥ 1.5M, Platinum ≥ 3M.

---

## 16. Luồng kỹ thuật đặt lịch (Client → API → Backend → DB)

> Đã đối chiếu tên class/method thật trong repo, không suy diễn tên hàm không tồn tại.

1. **Client:** React gửi `POST /api/Bookings` kèm `Authorization: Bearer <token>` (payload: serviceId, phone, licensePlate, scheduledTime, promoCode, rewardId).
2. **`Middleware/JwtMiddleware.cs`:** so `SessionId` trong token với `ActiveSessionId` trong DB; lệch → 401 Unauthorized (cơ chế single-session, xem cảnh báo mã BR ở mục 12/13); hợp lệ → gán `ClaimsPrincipal`, chuyển tiếp Controller.
3. **`BookingsController.cs`** (tên class đã verify đúng, số nhiều "Bookings"): nhận DTO, lấy `CustomerID` từ `User.FindFirst(ClaimTypes.NameIdentifier)`, gọi `BookingService.CreateBookingAsync`.
4. **`BookingService.cs` (Application layer):**
   - Kiểm tra quota Guest/Member/daily-limit (BR-25/26/27) và ràng buộc biển số (BR-28).
   - Khóa giao dịch: `AcquireBookingDateLockAsync(newStart.Date)` — advisory lock theo ngày, **chỉ có tác dụng trên PostgreSQL** (đã verify trong comment code `BookingService.cs:286-294`), chống race-condition 2 request cùng lúc.
   - Tính slot khả dụng theo `MaxParallelSlots`; nếu đầy thì xét `PriorityBufferSlots` theo thứ tự ưu tiên Tier.
   - Lưu `Booking` với `Status = Pending`, `SaveChangesAsync`.
5. **`SignalRAdminNotifier.cs`:** bắn `Clients.All.SendAsync("NewBooking", ...)` (tên event `"NewBooking"` đã verify đúng trong code) — Admin Dashboard nhận qua SignalR không cần F5. (Tránh nói con số tuyệt đối kiểu "0.1 giây" — không đo được, chỉ nên nói "gần như tức thời/real-time".)
6. **Trả kết quả:** `201 Created` kèm Booking DTO; FE chuyển hướng sang trang lịch sử đặt hàng.

**Exception đã verify trong code:**
- Double-payment: `PaymentService.cs:132-146` bắt `DbUpdateException` từ Postgres, kiểm tra `SqlState == "23505"` (unique-violation) → ném `"ALREADY_PAID"` → `AdminPaymentController.cs:38` trả **409 Conflict**. Đúng là chặn bằng **DB unique constraint thật**, không phải check tay ở tầng service.
- Auto no-show: `AutoNoShowJob.cs` — trễ 15 phút chưa check-in → chuyển `NoShow`; ≥ 3 lần no-show trong 30 ngày → khóa 15 ngày (khớp **BR-66**, không phải BR-67).
