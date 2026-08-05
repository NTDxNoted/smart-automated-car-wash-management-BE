# AutoWash Pro — Luồng chức năng & File code liên quan

> Tài liệu này liệt kê các luồng (flow) đang thực sự chạy được trong hệ thống, đối chiếu trực tiếp với source code Backend (`smart-automated-car-wash-management-BE`) và Frontend (`smart-automated-car-wash-management-FE`) tại thời điểm biên soạn. Dùng kèm `doc/SRS.md` (đặc tả chi tiết theo Use Case) và `doc/CONTEXT.md` (66 Business Rules).

---

## 1. Luồng Guest đặt lịch (không tài khoản)

- Xem dịch vụ (`ServicesPage.jsx`, `serviceService.js` → `ServiceController.cs`)
- Chọn xe/nhập biển số + chọn giờ (`StepSelectService.jsx`, `StepVehicleTime.jsx`, `TimeSlotGrid.jsx`, `TimeSlotItem.jsx` → `BookingsController.cs`, `BookingService.cs`)
- Xác nhận tạo booking Pending (`StepConfirm.jsx`, `InvoicePreview.jsx`, `bookingService.js` → `BookingsController.cs`, `BookingService.cs`)
- Admin nhận realtime (`signalrService.js` → `Hubs/AdminNotificationHub.cs`)

## 2. Luồng Member đầy đủ (vòng đời khách thân thiết)

- Đăng ký (`RegisterPage.jsx`, `authService.js` → `AuthController.cs`, `AuthService.cs`)
- Đăng nhập, single-session (`LoginPage.jsx`, `authService.js` → `AuthController.cs`, `AuthService.cs`, `Middleware/JwtMiddleware.cs`)
- Quản lý hồ sơ & xe (`ProfilePage.jsx`, `profileService.js`, `vehicleService.js` → `ProfileController.cs`, `VehicleController.cs`)
- Đặt lịch có mã khuyến mãi + đổi điểm (`BookingPage.jsx`, `PromoCodeInput.jsx`, `bookingService.js`, `loyaltyService.js` → `BookingsController.cs`, `PromotionsController.cs`, `RewardsController.cs`, `LoyaltyController.cs`)
- Xem ví điểm/lịch sử tích điểm (`LoyaltyPage.jsx`, `RewardCard.jsx`, `loyaltyService.js` → `LoyaltyController.cs`)
- Xem/huỷ lịch sử booking (`BookingHistoryPage.jsx`, `BookingCard.jsx`, `CancelConfirmDialog.jsx` → `BookingsController.cs`)

## 3. Luồng vận hành quầy của Admin

- Đăng nhập Admin (`LoginPage.jsx`, `adminAuthService.js` → `Admin/AdminAuthController.cs`, `AdminAuthService.cs`)
- Xem & xác nhận booking realtime (`BookingManagementPage.jsx`, `BookingTable.jsx`, `BookingDetailDrawer.jsx`, `StatusUpdateDropdown.jsx`, `adminBookingService.js` → `Admin/AdminBookingController.cs`, `AdminBookingService.cs`)
- Nhấn Dừng khẩn cấp khi sự cố (`EmergencyStopButton.jsx` → `BookingService.cs`)
- Ghi nhận thanh toán (`PaymentForm.jsx` → `adminBookingService.js` gọi `POST /admin/bookings/{id}/payment` → `Admin/AdminBookingController.cs`, `PaymentService.cs`, `Admin/AdminPaymentController.cs`)

## 4. Luồng Admin quản trị danh mục

- Quản lý dịch vụ (`ServiceManagementPage.jsx`, `ServiceModal.jsx`, `adminServiceService.js` → `Admin/AdminServiceController.cs`)
- Quản lý khuyến mãi (`PromotionManagementPage.jsx`, `PromotionModal.jsx`, `adminPromotionService.js` → `Admin/AdminPromotionsController.cs`, `PromotionService.cs`)
- Cấu hình hạng thành viên (`TierConfigPage.jsx`, `TierModal.jsx`, `adminTierService.js` → `Admin/AdminTierController.cs`)
- ⚠️ Quản lý Reward: `Admin/AdminRewardController.cs` đã có ở Backend nhưng **chưa tìm thấy trang/service riêng ở Frontend** tại thời điểm kiểm tra — cần xác nhận lại nếu đã làm ở nhánh khác chưa merge.

## 5. Luồng Admin quản lý khách hàng

- Danh sách/chi tiết khách hàng, khoá/mở tài khoản (`CustomerListPage.jsx`, `CustomerDetailPage.jsx`, `CustomerTable.jsx`, `CustomerDetailPanel.jsx`, `LockToggleButton.jsx`, `adminCustomerService.js` → `Admin/AdminCustomersController.cs`)

## 6. Luồng báo cáo (Reports)

- Dashboard tổng quan (`DashboardPage.jsx`, `OverviewChart.jsx`, `LoyaltyStatsPanel.jsx`, `TierDistributionChart.jsx`, `RfmTable.jsx` → `Admin/AdminReportController.cs`)
- Dịch vụ phổ biến (`PopularServicesReportPage.jsx`, `PopularServicesChart.jsx`, `PopularServicesTable.jsx`)
- Công suất trạm (`OccupancyReportPage.jsx`, `HourlyOccupancyChart.jsx`, `WeeklyOccupancyChart.jsx`, `BookingStatusPieChart.jsx`)
- ROI khuyến mãi (`PromotionsRoiReportPage.jsx`, `PromoRoiTable.jsx`, `PromoSummaryCards.jsx`)
- Tất cả các trang trên đều gọi chung `adminReportService.js` → `Admin/AdminReportController.cs`

## 7. Luồng chạy nền tự động (Background Jobs)

- Auto No-show (`Jobs/AutoNoShowJob.cs`)
- Tier Downgrade hàng tháng (`Jobs/TierDowngradeJob.cs`)
- Point Expiry (`Jobs/PointExpiryJob.cs`)

---

*Ghi chú: đường dẫn đầy đủ của các file BE nằm trong `src/AutoWashPro.API/...`, `src/AutoWash.Application/...`, `src/AutoWash.Infrastructure/...` (repo BE); file FE nằm trong `src/pages/...`, `src/components/...`, `src/services/...` (repo FE).*

---

# PHỤ LỤC A — Giải thích luồng Backend dễ hiểu (dùng khi trình bày với Giảng viên)

> Phần này diễn giải lại đúng những gì hệ thống đang làm ở Phụ lục B, nhưng bỏ hết code/tên hàm, viết bằng lời thường để thuyết trình hoặc trả lời câu hỏi của GVHD mà không cần mở source code.

## 1. Một yêu cầu từ trình duyệt phải đi qua mấy "trạm kiểm soát" trước khi được xử lý?

Mỗi khi người dùng bấm nút gì đó trên giao diện (đăng nhập, đặt lịch, xác nhận thanh toán...), yêu cầu đó được gửi lên Backend và phải lần lượt đi qua 3 lớp kiểm tra trước khi tới đúng chức năng xử lý:

1. **Kiểm tra nguồn gốc:** hệ thống xác nhận yêu cầu đến từ giao diện web hợp lệ (không phải để bảo mật tuyệt đối, mà để trình duyệt cho phép Frontend và Backend "nói chuyện" với nhau — 2 bên chạy ở 2 địa chỉ khác nhau lúc phát triển).
2. **Kiểm tra đăng nhập:** hệ thống đọc "vé vào cửa" (token) mà người dùng gửi kèm. Có một điểm đặc biệt: mỗi tài khoản chỉ được giữ **một vé còn hiệu lực tại một thời điểm**. Nếu tài khoản đó vừa đăng nhập ở một thiết bị/trình duyệt khác, vé cũ lập tức bị vô hiệu — người đang dùng vé cũ sẽ bị yêu cầu đăng nhập lại ngay ở yêu cầu tiếp theo. Đây là cơ chế chống một tài khoản bị dùng chung ở nhiều nơi cùng lúc.
3. **Kiểm tra quyền hạn:** nếu yêu cầu nhắm vào khu vực quản trị (Admin) nhưng người gửi chưa đăng nhập, hoặc đã đăng nhập nhưng không phải Admin, hệ thống chặn lại và trả lỗi tương ứng (chưa đăng nhập / không đủ quyền), tuyệt đối không cho đi tiếp vào chức năng xử lý.

Chỉ khi qua đủ cả 3 lớp, yêu cầu mới được chuyển tới đúng chức năng nghiệp vụ (đặt lịch, thanh toán, quản lý khách hàng...).

Ngoài ra, kênh gửi thông báo thời gian thực (realtime) có 2 "đường dây" tách biệt: một đường chỉ Admin đã đăng nhập mới nghe được (dùng để báo có đơn mới, khách mới đăng ký), một đường công khai ai xem trang đặt lịch cũng nghe được (dùng để cập nhật ngay khi một khung giờ vừa đầy chỗ hoặc vừa có người huỷ).

## 2. Đăng ký & Đăng nhập diễn ra như thế nào?

- **Đăng ký:** hệ thống kiểm tra số điện thoại chưa từng đăng ký, mã hoá mật khẩu trước khi lưu (không bao giờ lưu mật khẩu dạng chữ thường có thể đọc được), tạo tài khoản mới ở hạng thấp nhất, đồng thời tạo luôn một "ví điểm thưởng" rỗng cho tài khoản đó. Admin đang mở Dashboard sẽ được báo ngay có khách mới đăng ký mà không cần bấm làm mới trang.
- **Đăng nhập:** hệ thống xác nhận đúng số điện thoại + mật khẩu, kiểm tra tài khoản có đang bị khoá không, rồi phát hành một "vé vào cửa" mới — đồng thời hủy vé cũ như đã giải thích ở trên.

## 3. Đặt lịch rửa xe diễn ra theo trình tự nào?

1. Hệ thống kiểm tra ngay từ đầu: giờ hẹn phải cách hiện tại ít nhất 60 phút, nếu không đúng thì từ chối luôn, không cần kiểm tra gì thêm.
2. Phân biệt khách vãng lai và thành viên: khách vãng lai được hệ thống ngầm gán vào một "hồ sơ khách vãng lai dùng chung" để mọi lượt đặt đều có chỗ lưu vết, còn thành viên thì dùng đúng hồ sơ cá nhân, đồng thời bị chặn nếu tài khoản đang khoá hoặc đang trong thời gian bị phạt do vắng mặt nhiều lần.
3. Hệ thống kiểm tra hàng loạt điều kiện hạn mức: số đơn đang chờ tối đa theo loại khách, số đơn trong ngày, khoảng cách tối thiểu giữa 2 lần đặt của cùng một biển số — bất kỳ điều kiện nào không đạt sẽ bị từ chối ngay với lý do cụ thể.
4. Vì khung giờ cuối cùng có thể có nhiều người cùng bấm đặt một lúc, hệ thống tạm thời "giữ chỗ" khung giờ đó lại trong lúc xử lý, để đảm bảo không có 2 người cùng giành được 1 chỗ trống cuối cùng — xử lý xong mới nhả ra.
5. Hệ thống tự động cộng dồn 3 nguồn giảm giá nếu khách hàng đủ điều kiện: giảm theo hạng thành viên, giảm do đổi điểm thưởng (giới hạn không quá một nửa hoá đơn), và giảm theo mã khuyến mãi (có kiểm tra mã còn hạn, đúng đối tượng, chưa vượt số lượt dùng) — rồi ra số tiền cuối cùng khách phải trả.
6. Sau khi tạo đơn thành công, hệ thống lập tức báo cho Admin biết có đơn mới (không cần Admin tự làm mới trang), đồng thời cập nhật lại số chỗ trống của khung giờ đó cho tất cả mọi người đang xem trang đặt lịch.

## 4. Huỷ lịch hẹn hoạt động ra sao?

Khách chỉ huỷ được khi đơn còn đang chờ xử lý và còn cách giờ hẹn ít nhất 2 tiếng. Nếu lúc đặt có dùng điểm đổi thưởng, số điểm đó được hoàn lại vào ví điểm ngay khi huỷ thành công. Hệ thống cũng cập nhật lại ngay số chỗ trống của khung giờ vừa được giải phóng.

*Lưu ý nhỏ nhưng đáng nói với GVHD nếu được hỏi kỹ:* khi hoàn điểm, hệ thống hiện chưa giữ đúng ngày hết hạn gốc của số điểm đó như mô tả trong yêu cầu ban đầu — đây là điểm có thể cải thiện thêm nếu có thời gian.

## 5. Nhân viên tại quầy vận hành một đơn hàng như thế nào?

Khi xe tới trạm, Admin làm lần lượt: check-in (ghi nhận giờ xe thực tế đến — mốc này còn được dùng để tự động phát hiện "khách không đến"), đối chiếu biển số thực tế (sửa lại nếu có sai lệch), và nếu có sự cố kỹ thuật thì bấm "dừng khẩn cấp" để ghi log cảnh báo và chuyển đơn sang trạng thái thất bại. Sau khi rửa xe xong, Admin ghi nhận thanh toán (tiền mặt hoặc chuyển khoản); chỉ sau bước này đơn mới thật sự hoàn tất — lúc đó hệ thống mới cộng điểm thưởng và tính lại xem khách có đủ điều kiện lên hạng hay không. Hệ thống có chặn việc bấm xác nhận thanh toán 2 lần cho cùng một đơn.

*Lưu ý nhỏ:* hiện có nhiều hơn một cách trong hệ thống có thể đánh dấu một đơn là "hoàn tất", nhưng chỉ cách đi qua bước thanh toán mới lưu lại lịch sử giao dịch đầy đủ — nên trong thực tế nên luôn thao tác hoàn tất đơn thông qua màn hình xác nhận thanh toán.

## 6. Những việc gì hệ thống tự làm mà không cần Admin bấm nút?

- **Tự động đánh dấu "khách không đến":** hệ thống kiểm tra định kỳ mỗi phút, nếu một đơn đã quá 15 phút so với giờ hẹn mà khách vẫn chưa check-in thì tự động chuyển đơn đó sang "không đến"; nếu một tài khoản bị vậy đủ 3 lần trong 30 ngày, tài khoản đó bị tạm khoá quyền đặt lịch online 15 ngày.
- **Tự động hạ hạng thành viên:** đúng 0 giờ ngày đầu tiên mỗi tháng, hệ thống rà lại tổng chi tiêu 12 tháng gần nhất của toàn bộ thành viên để hạ hạng những ai không còn đủ điều kiện.
- **Tự động xoá điểm hết hạn:** mỗi ngày lúc nửa đêm, hệ thống rà và xoá các phần điểm thưởng đã quá 12 tháng kể từ ngày tích được.

Cả ba việc này chạy nền liên tục cùng lúc với hệ thống web, không cần ai bấm nút hay có mặt để kích hoạt.

---

# PHỤ LỤC B — Chi tiết kỹ thuật (code-level, dành cho Developer)

> Phần dưới đây mô tả chính xác thứ tự xử lý trong code Backend (đọc trực tiếp từ `Program.cs` và các `Service`/`Job`), không phải sơ đồ lý thuyết. Dùng khi cần debug hoặc đối chiếu với source code.

## A. Request Pipeline (thứ tự middleware — `Program.cs`)

```
Request vào
  → CORS ("FrontendDev" — allow-all origin, allow credentials)
  → JwtMiddleware                (đọc header Authorization, tự validate JWT + so khớp SessionId)
  → RoleAuthorizationMiddleware  (chỉ chặn path /api/admin/*, trừ /api/admin/auth/login)
  → UseAuthentication / UseAuthorization  (ASP.NET Core built-in, dùng chung JWT Bearer)
  → MapControllers / MapHub("/hubs/admin-notifications") / MapHub("/hubs/booking")
```

- **`JwtMiddleware`** không chỉ validate chữ ký JWT — nó còn tự tay đọc claim `SessionId` trong token, load `Customer` từ DB, và so sánh với `Customer.ActiveSessionId` hiện tại. Nếu lệch (do đăng nhập ở thiết bị khác ghi đè `ActiveSessionId`) → trả `401 { error: "SESSION_EXPIRED" }` **ngay tại middleware**, request không tới được Controller. Đây chính là cơ chế Single Concurrent Session.
- **SignalR qua JWT**: trình duyệt không set được header `Authorization` khi bắt tay WebSocket, nên `OnMessageReceived` trong `Program.cs` đọc token từ query string `?access_token=...` **chỉ cho path `/hubs/*`**, request API thường vẫn dùng header như cũ.
- Có **2 SignalR Hub** khác mục đích:
  - `AdminNotificationHub` (`/hubs/admin-notifications`) — `[Authorize(Roles = "ADMIN")]`, 1 chiều Server→Client, chỉ Admin connect được.
  - `BookingHub` (`/hubs/booking`) — public, đẩy số chỗ trống theo slot cho mọi client đang xem trang đặt lịch (kể cả Guest).

## B. Luồng Đăng ký / Đăng nhập (`AuthService.cs`)

**Register:** validate field → check `Phone` trùng → `BCrypt.Net.BCrypt.HashPassword` → insert `Customer(Role="MEMBER", TierID mặc định)` → insert `LoyaltyAccount(TotalPoints=0)` → nếu có `IAdminNotifier`, gọi `NotifyNewCustomerAsync` đẩy realtime cho Admin đang online.

**Login:** tìm `Customer` theo `Phone` → chặn nếu `IsLocked` → `BCrypt.Verify` → resolve tên Tier hiện tại từ `TierID` (vì `Customer.Tier` là field `[NotMapped]`) → **sinh `ActiveSessionId` mới (GUID) và ghi đè vào DB** (điều này tự động vô hiệu hoá mọi JWT cũ đang cầm `SessionId` khác) → phát hành JWT chứa claim `NameIdentifier, Name, phone, tier, Role, SessionId` → trả `AuthResponse`.

## C. Luồng tạo Booking (`BookingService.CreateBookingAsync` — `POST /api/Bookings`)

Thứ tự xử lý thật trong code:

1. Validate `ServiceId > 0` và `ScheduledTime ≥ now + 60 phút` (BR-29) — **fail-fast trước khi đụng DB**.
2. Rẽ nhánh **Member** (`customerId` có giá trị) vs **Guest**:
   - *Member*: load `Customer` → chặn nếu `IsLocked` hoặc `SuspendedUntil > now` (đang bị phạt no-show) → load `Tier` + `LoyaltyAccount` → resolve xe (theo `VehicleId` có sẵn, hoặc validate + tạo mới biển số qua `LicensePlateValidator`) → chặn nếu `ScheduledTime` vượt `Tier.BookingWindowDays`.
   - *Guest*: bắt buộc `Phone` + `LicensePlate` hợp lệ → tìm-hoặc-tạo **1 bản ghi Customer giả dùng chung** (`Phone = "GUEST"`, `FullName = "Khách vãng lai"`) để mọi Guest booking có `CustomerID` hợp lệ → tìm-hoặc-tạo Vehicle → Tier fallback dựng tạm trong memory (`TierID=1`, không insert DB) nếu chưa seed.
3. Kiểm tra hạn mức: đếm `pendingCount` (Guest ≤ 1, Member ≤ 3 — BR-25/26), đếm `sameDayCount` Pending trong ngày (≤ 2 — BR-27), kiểm tra buffer 120 phút cùng biển số (BR-28).
4. Tính `newStart`/`newEnd` (= duration dịch vụ + 5 phút đệm).
5. **Kiểm soát tương tranh (TOCTOU)**: mở `DbContext` transaction + gọi `AcquireBookingDateLockAsync(date)` — advisory lock của PostgreSQL theo ngày, đảm bảo 2 request đặt cùng slot cuối không cùng lọt qua kiểm tra overlap.
6. Đếm lại `overlapCount` slot đó; so với `MaxParallelSlots` (config `BookingSettings`, mặc định 3). Nếu đầy, chỉ khách có `Tier.PriorityScore` cao hơn mức thấp nhất mới được cấp thêm `PriorityBufferSlots` (BR-19), ngược lại từ chối `SLOT_NOT_AVAILABLE`.
7. Tính giá: `tierDiscount` = base × `DiscountRate`%; `rewardDiscount` (nếu có `RewardId`) theo % hoặc số tiền cố định, **cap tối đa 50% hoá đơn** (BR-60); `promotionDiscount` (nếu có `PromoCode`) — validate mã tồn tại/active, còn hạn (so theo **giờ local UTC+7**), đủ Tier tối thiểu, chưa vượt `MaxUsage` toàn hệ thống, **mỗi khách chỉ dùng 1 lần/mã**, đơn đạt `MinOrderValue`, cap `MaxDiscountAmount`.
8. `FinalAmount = max(0, Base - (TierDiscount + RewardDiscount + PromotionDiscount))` (BR-39) → insert `Booking(Status = Pending)` → `SaveChanges` → **commit transaction** (giải phóng advisory lock).
9. Sau commit: trừ điểm Reward ngay lập tức (ghi `PointTransaction` loại `Redeem`), ghi nhận `CustomerPromotion` đã dùng mã.
10. Bắn 2 sự kiện realtime: `IAdminNotifier.NotifyNewBookingAsync` (Admin Dashboard) và `IBookingHubNotifier.NotifySlotOccupancyChangedAsync` (cập nhật số chỗ trống cho mọi client trang booking).

## D. Luồng Huỷ Booking (`CancelBookingAsync` — `POST /api/Bookings/{id}/cancel`)

Check quyền sở hữu (`customerId`/`guestPhone` khớp) → chỉ cho huỷ khi `Status == Pending` và còn ≥ 2 tiếng tới giờ hẹn (BR-63) → set `Cancelled` → nếu có `PointsRedeemed > 0`, hoàn điểm bằng cách ghi thêm 1 `PointTransaction` loại `Earn` (**lưu ý:** bản ghi hoàn điểm này không set lại `ExpiredAt` gốc như lúc tích điểm ban đầu — khác với mô tả "giữ nguyên hạn dùng gốc" của BR-62, nên kiểm tra lại nếu cần đúng 100% BR khi chấm điểm) → tính lại `overlapCount` slot đó → bắn `NotifySlotOccupancyChangedAsync` để giải phóng chỗ trên UI mọi client.

## E. Luồng nghiệp vụ tại quầy của Admin (`AdminBookingService.cs`, `AdminBookingController.cs`)

| Endpoint | Method | Điều kiện | Hiệu ứng |
|---|---|---|---|
| `PATCH /api/admin/bookings/{id}/status` | `UpdateBookingStatusAsync` | Chỉ chuyển được khi đang `Pending` | Nếu `NewStatus = Completed` → cộng `TotalSpending`, gọi `TierService.EvaluateUpgradeAsync` + `PointService.EarnPointsAsync`; nếu `NoShow` → gọi `ApplyNoShowPenaltyAsync` (đếm no-show 30 ngày gần nhất, ≥3 lần thì `IsLocked=true` + `SuspendedUntil=+15 ngày` — BR-66); mọi trường hợp đều bắn `NotifyBookingStatusChangedAsync` |
| `PATCH /api/admin/bookings/{id}/license-plate` | `UpdateLicensePlateAsync` | Chỉ khi `Pending` | Sửa trực tiếp `LicensePlate` (dùng khi đối soát phát hiện sai biển số — BR-44) |
| `POST /api/admin/bookings/{id}/checkin` | `CheckInAsync` | Chỉ khi `Pending` | Ghi `CheckInTime = now` — đây là mốc để `AutoNoShowJob` và `EmergencyStopAsync` tham chiếu |
| `POST /api/admin/bookings/{id}/emergency-stop` | `EmergencyStopAsync` | Bắt buộc đã `CheckIn` và còn `Pending` | `Log.Error` cảnh báo (BR-43) → set `Failed` → bắn `NotifyBookingStatusChangedAsync` |
| `POST /api/admin/bookings/{id}/payment` | `PaymentService.RecordPaymentAsync` (`AdminPaymentController`, controller khác nhưng **cùng route prefix** `api/admin/bookings`) | Chỉ khi `Pending`, đúng `Cash`/`Transfer`, `Confirmed = true` | Xem mục F |

> ⚠️ **Quan sát về code:** hiện có **3 đường** khác nhau có thể đưa booking sang `Completed` và cộng điểm/tier: (1) `BookingService.CompleteBookingAsync` (`POST /api/Bookings/{id}/complete`), (2) `AdminBookingService.UpdateBookingStatusAsync` khi `NewStatus=Completed`, và (3) `PaymentService.RecordPaymentAsync`. Chỉ đường (3) tạo bản ghi `Transaction` (lịch sử thanh toán) và có chống thanh toán trùng qua unique constraint DB. FE hiện tại (`PaymentForm.jsx`) gọi đúng đường (3). Nên rà lại xem đường (1) có còn được FE nào gọi tới không, tránh Completed một đơn mà không có `Transaction` tương ứng.

## F. Luồng Thanh toán offline (`PaymentService.RecordPaymentAsync`)

1. Validate booking tồn tại, `Status == Pending`, `PaymentMethod` hợp lệ (`Cash`/`Transfer`), `Confirmed == true` (nếu Cash mà chưa tick "đã thu tiền" → `CASH_NOT_CONFIRMED`, BR-47).
2. Mở DB transaction: insert `Transaction(PaidAt = UtcNow, Status = Paid)` → `SaveChanges` **ngay lập tức** để cố tình kích hoạt lỗi unique-index sớm nếu đơn đã có giao dịch trước đó (chống thanh toán 2 lần — BR-48).
3. Set `Booking.Status = Completed`, cộng `Customer.TotalSpending`, cập nhật `LastVisit`.
4. Gọi `PointService.EarnPointsAsync` (công thức `MIN(FLOOR(FinalAmount/10000), 500)` — BR-51/53, set `ExpiredAt = +12 tháng` — BR-55) → `SaveChanges` → gọi `TierService.EvaluateUpgradeAsync` (BR-21) → `transaction.CommitAsync()`.
5. Nếu `DbUpdateException` với `SqlState == "23505"` (Postgres unique violation) → rollback, trả lỗi nghiệp vụ `ALREADY_PAID` thay vì lỗi 500 thô.
6. Bắn `NotifyBookingStatusChangedAsync` (Pending → Completed) cho Admin Dashboard.

## G. Background Jobs (`BackgroundService`, không dùng Hangfire/Quartz — tự viết vòng lặp `Task.Delay`)

| Job | Cơ chế lập lịch thật trong code | Nghiệp vụ |
|---|---|---|
| `AutoNoShowJob` | `while(true) { ...xử lý...; await Task.Delay(1 phút); }` — quét liên tục mỗi 60 giây | Tìm `Booking` còn `Pending`, `ScheduledTime ≤ now-15 phút`, chưa `CheckInTime` → set `NoShow` (BR-65); đồng thời tự đếm no-show 30 ngày gần nhất **ngay trong job này** (không gọi lại `AdminBookingService`) để suspend tài khoản (BR-66) |
| `TierDowngradeJob` | Tính `TimeSpan` tới đúng 00:00 UTC ngày 1 tháng sau, `Task.Delay` tới đúng mốc đó rồi mới chạy `ITierService.RunMonthlyDowngradeAsync()` (BR-22); có `SemaphoreSlim` chặn chạy trùng trong cùng process |
| `PointExpiryJob` | Tương tự, `Task.Delay` tới đúng 00:00 UTC mỗi ngày rồi chạy `IPointService.RunDailyExpiryAsync()` (BR-57) |

> Cả 3 job đăng ký qua `builder.Services.AddHostedService<...>()` trong `Program.cs` — chạy trong **cùng process** với Web API (không phải worker/service tách rời), nên nếu API restart, job cũng restart theo và tính lại delay từ đầu.

---
