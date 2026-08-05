# Luồng FE ↔ BE theo từng Issue (Admin Panel)

> Tài liệu này liệt kê luồng chạy thực tế (Frontend gọi gì → Backend xử lý ra sao) cho đúng nhóm issue Admin đã đóng gần đây:
> **FE-ISSUE-08** (Dashboard & Booking Management), **FE-ISSUE-09** (Customer Management), **FE-ISSUE-10** (Service, Promotion & Tier Config), **FE-ISSUE-11** (Reports & RFM Dashboard), **FE-ISSUE-14** (Popular Services Report), **FE-ISSUE-15** (Peak Occupancy Report), **FE-ISSUE-16** (Promotion ROI Report).
> Xem thêm `doc/FLOWS.md` (toàn bộ hệ thống, cả Guest/Member) và `doc/CONTEXT.md` (Business Rules).

---

# PHẦN 1 — GIẢI THÍCH BẰNG TIẾNG VIỆT (dùng khi trình bày với Giảng viên)

> Phần này không dùng tên hàm/tên file — chỉ trả lời 3 câu hỏi cho mỗi chức năng: *dùng để làm gì, số liệu lấy từ đâu, và luồng chạy từng bước ở FE/BE ra sao*. Phần code-level chi tiết nằm ở PHẦN 2 bên dưới.

### Bảng tra nhanh

| Issue | Chức năng |
|:---|:---|
| FE-ISSUE-08 | Dashboard tổng quan & Quản lý booking |
| FE-ISSUE-09 | Quản lý khách hàng |
| FE-ISSUE-10 | Cấu hình Dịch vụ, Khuyến mãi & Hạng thành viên |
| FE-ISSUE-11 | Báo cáo khách hàng (RFM) & phân bố hạng thành viên |
| FE-ISSUE-14 | Báo cáo Dịch vụ phổ biến |
| FE-ISSUE-15 | Báo cáo Khung giờ/Ngày cao điểm |
| FE-ISSUE-16 | Báo cáo Hiệu quả khuyến mãi (ROI) |

---

## FE-ISSUE-08 · Dashboard tổng quan & Quản lý booking

- **Dùng để làm gì:** Cho Admin xem nhanh tình hình kinh doanh trong ngày/tuần/tháng, đồng thời trực tiếp xử lý từng đơn đặt lịch (nhận xe, xác nhận, huỷ, báo sự cố) ngay khi khách đặt.
- **Số liệu lấy từ đâu:** Đếm trực tiếp từ các đơn đặt lịch trong khoảng thời gian được chọn; danh sách đơn cập nhật tức thời qua kênh thông báo thời gian thực, không cần bấm làm mới trang.

**Luồng FE:**
1. Admin đăng nhập vào trang quản trị.
2. Mở trang Dashboard → hệ thống tự tải số liệu tổng quan (số đơn, doanh thu, tỉ lệ khách không đến...) theo khoảng thời gian đang chọn (ngày/tuần/tháng/năm).
3. Chuyển sang trang Quản lý booking → thấy danh sách toàn bộ đơn đặt lịch, lọc/tìm theo nhu cầu.
4. Khi khách tới trạm, Admin bấm "Check-in" để ghi nhận giờ xe đến.
5. Rửa xong, Admin xác nhận trạng thái đơn hoặc ghi nhận thanh toán.
6. Nếu có sự cố kỹ thuật, Admin bấm "Dừng khẩn cấp".
7. Toàn bộ màn hình tự cập nhật ngay khi có đơn mới hoặc đổi trạng thái, không cần bấm làm mới trang.

**Luồng BE:**
1. Nhận yêu cầu lấy số liệu tổng quan → đếm số đơn theo từng trạng thái (hoàn tất/thất bại/không đến/đã huỷ) trong khoảng thời gian, tính doanh thu, tỉ lệ khách không đến, giá trị đơn trung bình.
2. Nhận yêu cầu danh sách/chi tiết đơn → trả về dữ liệu đơn đặt lịch tương ứng.
3. Khi Admin check-in → lưu lại giờ xe đến, dùng làm mốc để sau này tự động phát hiện "khách không đến".
4. Khi đơn chuyển sang hoàn tất → tự cộng điểm thưởng, cộng tổng chi tiêu, và kiểm tra khách có đủ điều kiện lên hạng thành viên không.
5. Khi Admin bấm dừng khẩn cấp → ghi log cảnh báo và chuyển đơn sang trạng thái thất bại.
6. Mỗi khi có thay đổi, hệ thống gửi ngay một thông báo qua kênh thời gian thực để mọi màn hình Admin đang mở tự cập nhật.

---

## FE-ISSUE-09 · Quản lý khách hàng

- **Dùng để làm gì:** Tra cứu thông tin khách hàng, xem lịch sử, khoá tài khoản nếu vi phạm (ví dụ bỏ hẹn nhiều lần).
- **Số liệu lấy từ đâu:** Đọc trực tiếp từ hồ sơ khách hàng lưu trong hệ thống; khoá tài khoản có hiệu lực ngay lập tức, khách bị khoá sẽ không đặt lịch được nữa.

**Luồng FE:**
1. Admin mở trang danh sách khách hàng → xem toàn bộ khách, tìm theo tên/số điện thoại.
2. Bấm vào một khách → xem trang chi tiết: thông tin cá nhân, lịch sử đặt lịch, hạng thành viên, điểm thưởng.
3. Nếu phát hiện khách vi phạm (bỏ hẹn nhiều lần, gian lận...), Admin bấm khoá tài khoản; có thể mở khoá lại khi cần.

**Luồng BE:**
1. Nhận yêu cầu danh sách → trả về hồ sơ khách hàng, hỗ trợ tìm kiếm.
2. Nhận yêu cầu chi tiết 1 khách → trả về đầy đủ thông tin khách đó.
3. Khi Admin bấm khoá/mở khoá → cập nhật trạng thái khoá ngay lập tức; từ lúc này khách bị khoá sẽ không đặt lịch được nữa cho tới khi được mở lại.

---

## FE-ISSUE-10 · Cấu hình Dịch vụ, Khuyến mãi & Hạng thành viên

- **Dùng để làm gì:** Cho Admin tự thêm/sửa dịch vụ rửa xe, tạo mã giảm giá, và chỉnh mức ưu đãi theo từng hạng thành viên mà không cần sửa code.
- **Số liệu lấy từ đâu:** Thay đổi được lưu ngay vào cấu hình hệ thống và áp dụng cho lượt đặt lịch tiếp theo — ví dụ đổi % giảm giá của hạng Vàng sẽ ảnh hưởng ngay đến hoá đơn của khách hạng Vàng đặt lịch sau đó.

**Luồng FE:**
1. Admin mở trang Quản lý dịch vụ → thêm dịch vụ mới hoặc sửa giá/thời lượng, ẩn/hiện một dịch vụ.
2. Mở trang Quản lý khuyến mãi → tạo mã giảm giá mới (hạn dùng, số lượt tối đa, điều kiện áp dụng), sửa mã cũ, bật/tắt một mã.
3. Mở trang Cấu hình hạng thành viên → chỉnh % giảm giá, điều kiện của từng hạng (Member, Bạc, Vàng, Bạch kim...).

**Luồng BE:**
1. Khi Admin lưu dịch vụ/khuyến mãi/hạng → kiểm tra dữ liệu hợp lệ rồi lưu ngay vào cấu hình hệ thống.
2. Từ thời điểm lưu, mọi lượt đặt lịch tiếp theo của khách sẽ áp dụng đúng cấu hình mới nhất, không cần khởi động lại hệ thống — ví dụ đổi mức giảm giá hạng Vàng sẽ tính đúng cho hoá đơn kế tiếp của khách hạng Vàng.
3. Khi Admin xem "số lượt đã dùng" của một mã khuyến mãi → đếm lại từ lịch sử các đơn đã dùng mã đó; đây cũng chính là dữ liệu gốc để tính báo cáo ROI khuyến mãi (Issue-16).

⚠️ Ghi chú: `AdminRewardController` đã có ở BE nhưng chưa xác nhận có trang quản lý riêng ở FE — không thuộc phạm vi issue này nhưng cần lưu ý nếu được hỏi.

---

## FE-ISSUE-11 · Báo cáo khách hàng (RFM) & phân bố hạng thành viên

- **Dùng để làm gì:** Giúp Admin nhận diện khách hàng thân thiết, khách có nguy cơ rời bỏ, và biết cơ cấu khách theo từng hạng thành viên.
- **Số liệu lấy từ đâu:** Tính từ lịch sử toàn bộ các đơn đã hoàn tất của từng khách (bao lâu chưa quay lại, quay lại bao nhiêu lần, tổng chi tiêu).

**Luồng FE:**
1. Admin mở trang Dashboard, xem phần "Khách hàng" → bảng xếp hạng khách theo mức độ gắn bó (RFM) và biểu đồ phân bố khách theo từng hạng thành viên.
2. Xem thêm khối thống kê điểm thưởng: tổng điểm đang lưu hành, điểm sắp hết hạn, điểm đã hết hạn.

**Luồng BE:**
1. Tính cho từng khách: bao lâu chưa quay lại, quay lại bao nhiêu lần, tổng tiền đã chi — dựa trên toàn bộ lịch sử đơn đã hoàn tất, sắp xếp khách chi tiêu nhiều nhất lên đầu.
2. Đếm số khách theo từng hạng thành viên thật (hạng do Admin cấu hình ở Issue-10), không suy ra hạng từ điểm thưởng.
3. Cộng tổng điểm thưởng toàn hệ thống, tính điểm sẽ hết hạn trong 7 ngày tới và điểm đã hết hạn.

---

## FE-ISSUE-14 · Báo cáo Dịch vụ phổ biến

- **Dùng để làm gì:** Cho biết dịch vụ nào được khách chọn nhiều nhất, mang lại doanh thu cao nhất trong một khoảng thời gian tự chọn.
- **Số liệu lấy từ đâu:** Chỉ tính trên các đơn đã hoàn tất (đã thanh toán xong) trong khoảng ngày được chọn, không tính đơn đang chờ hoặc đã huỷ.

**Luồng FE:**
1. Admin mở trang Báo cáo dịch vụ phổ biến → chọn khoảng ngày muốn xem.
2. Hệ thống hiển thị bảng xếp hạng dịch vụ theo số lượt dùng và biểu đồ trực quan, kèm doanh thu và % đóng góp của từng dịch vụ.

**Luồng BE:**
1. Chỉ lấy các đơn đã hoàn tất trong khoảng ngày được chọn.
2. Gom nhóm theo từng loại dịch vụ, đếm số lượt dùng, cộng doanh thu.
3. Tính % đóng góp của từng dịch vụ trên tổng số lượt, sắp xếp dịch vụ được dùng nhiều nhất lên đầu, trả kết quả cho FE vẽ bảng/biểu đồ.

---

## FE-ISSUE-15 · Báo cáo Khung giờ/Ngày cao điểm

- **Dùng để làm gì:** Giúp Admin biết những khung giờ và ngày trong tuần nào đông khách nhất, để cân nhắc xếp thêm nhân sự hoặc mở thêm chỗ.
- **Số liệu lấy từ đâu:** Đếm số lượt đặt lịch rơi vào từng khung giờ 30 phút và từng thứ trong tuần, có quy đổi đúng theo múi giờ Việt Nam trước khi thống kê.

**Luồng FE:**
1. Admin mở trang Báo cáo khung giờ cao điểm → chọn khoảng ngày (bắt buộc).
2. Xem biểu đồ theo thứ trong tuần (ngày nào đông nhất) và biểu đồ theo khung giờ 30 phút (giờ nào đông nhất), kèm biểu đồ tỉ lệ trạng thái đơn.

**Luồng BE:**
1. Lấy các đơn đã hoàn tất hoặc đang chờ trong khoảng ngày, quy đổi đúng theo giờ Việt Nam trước khi thống kê (tránh lệch múi giờ).
2. Đếm số đơn rơi vào từng thứ trong tuần (Thứ 2 → Chủ nhật).
3. Đếm số đơn rơi vào từng khung 30 phút trong ngày làm việc (7h30–17h30), tính tỉ lệ lấp đầy của từng khung.
4. Trả toàn bộ số liệu về cho FE vẽ biểu đồ.

---

## FE-ISSUE-16 · Báo cáo Hiệu quả khuyến mãi (ROI)

- **Dùng để làm gì:** Đánh giá một chương trình khuyến mãi có "lời" hay không — bỏ ra bao nhiêu tiền giảm giá thì thu lại được bao nhiêu doanh thu.
- **Số liệu lấy từ đâu:** Chỉ tính các đơn đã hoàn tất **và đã thanh toán thành công** có sử dụng mã khuyến mãi trong khoảng ngày được chọn. Công thức: `ROI = (doanh thu thu về − tiền đã giảm) / tiền đã giảm`.

**Luồng FE:**
1. Admin mở trang Báo cáo ROI khuyến mãi → chọn khoảng ngày.
2. Xem bảng từng mã khuyến mãi: số lượt dùng, tổng tiền đã giảm, doanh thu mang lại, tỉ lệ hiệu quả, kèm 2 thẻ tổng hợp tổng tiền giảm/tổng doanh thu.

**Luồng BE:**
1. Chỉ xét các đơn đã hoàn tất **và** đã thanh toán thành công, có sử dụng mã khuyến mãi, trong khoảng ngày được chọn.
2. Gom theo từng mã khuyến mãi: đếm số lượt dùng, cộng tổng tiền đã giảm, cộng tổng doanh thu các đơn đó mang lại.
3. Tính tỉ lệ hiệu quả = (doanh thu thu về − tiền đã giảm) / tiền đã giảm, sắp xếp mã mang lại doanh thu cao nhất lên đầu.

> **Điểm chung đáng lưu ý:** cả 3 báo cáo mới (Popular Services, Peak Occupancy, Promotion ROI) đều cho phép Admin **tự chọn khoảng ngày** để xem, và hệ thống luôn kiểm tra ngày bắt đầu phải trước ngày kết thúc, khoảng chọn không được vượt quá 1 năm — tránh việc quét dữ liệu quá lớn làm chậm hệ thống.

---

# PHẦN 2 — CHI TIẾT CODE-LEVEL (dành cho Developer)

> Đối chiếu trực tiếp với source code: FE gọi service nào, BE controller/service nào xử lý.

## FE-ISSUE-08 — Dashboard & Booking Management

**FE:**
- `DashboardPage.jsx` gọi `adminReportService.js` (`getOverviewReport`, `getRfmReport`, `getTierDistribution`, `getLoyaltyStats`) để vẽ `OverviewChart.jsx`, `TierDistributionChart.jsx`, `LoyaltyStatsPanel.jsx`.
- `BookingManagementPage.jsx` + `BookingTable.jsx`, `BookingDetailDrawer.jsx`, `StatusUpdateDropdown.jsx`, `EmergencyStopButton.jsx` gọi `adminBookingService.js`.
- Nhận cập nhật realtime qua `signalrService.js` (kênh `AdminNotificationHub`) khi có booking mới hoặc đổi trạng thái.

**BE:**
- `Admin/AdminReportController` (`GET /api/admin/reports/overview`) → `ReportService.GetOverviewReportAsync`: lọc booking theo `filterType` (day/week/month/year) hoặc `startDate/endDate` tùy chọn, tính `TotalBookings/Completed/Failed/NoShow/Cancelled/TotalRevenue/NoShowRate/AvgOrderValue`.
- `Admin/AdminBookingController` (`GET /api/admin/bookings`, `GET /{id}`, `PATCH /{id}/status`, `PATCH /{id}/license-plate`, `POST /{id}/checkin`, `POST /{id}/emergency-stop`) → `AdminBookingService.cs`. Chi tiết logic từng thao tác (check-in, dừng khẩn cấp, đổi trạng thái → cộng điểm/tier, áp phạt no-show) đã mô tả kỹ trong `FLOWS.md` mục E.
- Mọi thay đổi trạng thái booking bắn `NotifyBookingStatusChangedAsync`, booking mới bắn `NotifyNewBookingAsync` → FE nhận qua SignalR không cần refresh.

---

## FE-ISSUE-09 — Customer Management

**FE:**
- `CustomerListPage.jsx` + `CustomerTable.jsx` → danh sách khách hàng, tìm kiếm/lọc.
- `CustomerDetailPage.jsx` + `CustomerDetailPanel.jsx`, `LockToggleButton.jsx` → xem chi tiết, khoá/mở tài khoản.
- Cả hai trang dùng chung `adminCustomerService.js`.

**BE:**
- `Admin/AdminCustomersController`:
  - `GET /api/admin/customers` — danh sách (phân trang/tìm kiếm).
  - `GET /api/admin/customers/{id}` — chi tiết 1 khách hàng.
  - `PATCH /api/admin/customers/{id}/lock` — khoá/mở khoá (`IsLocked`), ảnh hưởng trực tiếp tới khả năng đặt lịch (xem BR liên quan `IsLocked` trong `BookingService.CreateBookingAsync`, `FLOWS.md` mục C.2).

---

## FE-ISSUE-10 — Service, Promotion & Tier Config

**FE:**
- `ServiceManagementPage.jsx` + `ServiceModal.jsx` → `adminServiceService.js`.
- `PromotionManagementPage.jsx` + `PromotionModal.jsx` → `adminPromotionService.js`.
- `TierConfigPage.jsx` + `TierModal.jsx` → `adminTierService.js`.

**BE:**
- `Admin/AdminServiceController` (`api/admin/services`): `POST` tạo dịch vụ, `PUT /{id}` sửa, `PATCH /{id}/status` bật/tắt hiển thị dịch vụ.
- `Admin/AdminPromotionsController` (`api/admin/promotions`): `GET` danh sách, `POST` tạo mã mới, `PUT /{id}` sửa, `PATCH /{id}/toggle` bật/tắt active, `GET /{id}/usage` xem số lượt đã dùng — dữ liệu này chính là nguồn cho báo cáo ROI ở FE-ISSUE-16.
- `Admin/AdminTierController` (`api/admin/tiers`): `GET` danh sách hạng, `PUT /{id}` sửa cấu hình hạng (`DiscountRate`, `BookingWindowDays`, ngưỡng chi tiêu...) — các giá trị này được `BookingService` và `TierService` đọc trực tiếp khi tính giá/đánh giá lên hạng (`FLOWS.md` mục C.7, `TierDowngradeJob`).
- ⚠️ Ghi chú tồn tại từ `FLOWS.md`: `Admin/AdminRewardController` đã có ở Backend nhưng **chưa tìm thấy trang/service riêng ở Frontend** tại thời điểm kiểm tra.

---

## FE-ISSUE-11 — Reports & RFM Dashboard

**FE:**
- `DashboardPage.jsx` là nơi tổng hợp (`RfmTable.jsx`, `TierDistributionChart.jsx`, `LoyaltyStatsPanel.jsx`), gọi `adminReportService.js`: `getRfmReport`, `getTierDistribution`, `getLoyaltyStats`.

**BE:**
- `GET /api/admin/reports/rfm` → `ReportService.GetRfmReportAsync`: đọc trực tiếp từ **view SQL** `VwCustomerRfm` (không tự tính trong code C#) trả về `RecencyDays/Frequency/MonetaryTotal/TotalPoints/TotalSpending/MemberSince`, sắp xếp theo `MonetaryTotal` giảm dần.
- `GET /api/admin/reports/tier-distribution` → `ReportService.GetTierDistributionAsync`: nhóm khách theo `Customer.TierID` thật (bảng `Tiers`, admin cấu hình được ở FE-ISSUE-10) — **không** suy ra hạng từ điểm thưởng, đây là 2 trục dữ liệu độc lập.
- `GET /api/admin/reports/loyalty-stats` → `ReportService.GetLoyaltyStatsAsync`: cộng `TotalPoints` toàn hệ thống, tính điểm sắp hết hạn trong 7 ngày tới và điểm đã hết hạn, dựa trên `PointTransaction.ExpiredAt`.

---

## FE-ISSUE-14 — Popular Services Report Page

**FE:**
- `PopularServicesReportPage.jsx` + `PopularServicesChart.jsx`, `PopularServicesTable.jsx` → `adminReportService.getPopularServices(startDate, endDate)`.
- FE tự đánh số `ranking` theo thứ tự trả về (BE đã sort sẵn) và map field `usageCount → totalWashes`, `percentage → revenueContribution`.

**BE:**
- `GET /api/admin/reports/popular-services?startDate&endDate` (validate: `startDate ≤ endDate`, khoảng ngày ≤ 366 ngày) → `ReportService.GetPopularServicesReportAsync`:
  1. Lọc `Booking.Status == Completed` trong khoảng ngày (theo `CreatedAt`, nếu không truyền ngày thì lấy toàn bộ).
  2. Gom nhóm theo `ServiceID`, đếm `UsageCount`, cộng `TotalRevenue = Sum(FinalAmount)`.
  3. Tính `Percentage` = tỉ lệ lượt dùng dịch vụ đó / tổng lượt hoàn tất, sắp xếp giảm dần theo `UsageCount`.

---

## FE-ISSUE-15 — Peak Occupancy Report Page

**FE:**
- `OccupancyReportPage.jsx` + `HourlyOccupancyChart.jsx`, `WeeklyOccupancyChart.jsx`, `BookingStatusPieChart.jsx` → `adminReportService.getPeakOccupancy(startDate, endDate)`.
- FE tự tính lại `occupancyRate` theo tuần (`count / weeksCount / maxParallelSlots`) từ dữ liệu thô BE trả, riêng tỉ lệ theo khung giờ dùng thẳng `occupancyPercentage` BE đã tính sẵn.

**BE:**
- `GET /api/admin/reports/peak-occupancy?startDate&endDate` (bắt buộc cả 2 tham số, validate `startDate ≤ endDate` và khoảng ≤ 366 ngày) → `ReportService.GetPeakOccupancyReportAsync`:
  1. Lấy booking `Completed` hoặc `Pending` có `ScheduledTime` trong khoảng — **chuyển biên ngày VN (UTC+7) sang UTC trước khi query** vì `ScheduledTime` lưu UTC (tránh lệch giờ 00:00–07:00).
  2. Thống kê theo **thứ trong tuần** (Thứ 2 → Chủ nhật): đếm booking rơi vào từng thứ.
  3. Thống kê theo **khung giờ 30 phút** từ 07:30 đến 17:30: đếm số booking mỗi khung, tính `OccupancyPercentage = count / (totalDays × MaxParallelSlots) × 100` (`MaxParallelSlots` hard-code = 1 trong report này, khác với cấu hình `BookingSettings.MaxParallelSlots` dùng lúc tạo booking — hai giá trị **không** liên thông, cần lưu ý nếu bị hỏi vặn).

---

## FE-ISSUE-16 — Promotion ROI Report Page

**FE:**
- `PromotionsRoiReportPage.jsx` + `PromoRoiTable.jsx`, `PromoSummaryCards.jsx` → `adminReportService.getPromotionsRoi(startDate, endDate)`.
- FE tự cộng dồn `totalDiscount`/`totalRevenue` từ danh sách `items` BE trả về để hiển thị summary card.

**BE:**
- `GET /api/admin/reports/promotions-roi?startDate&endDate` (validate như 2 report trên) → `ReportService.GetPromotionRoiReportAsync`:
  1. Join 4 bảng: `CustomerPromotions` (lượt dùng mã) ⋈ `Promotions` ⋈ `Bookings` ⋈ `Transactions`.
  2. Điều kiện: `Booking.Status == Completed`, `Transaction.Status == Paid`, `Booking.CompletedAt` nằm trong khoảng ngày (đã dịch biên UTC+7 → UTC).
  3. Gom theo từng `Promotion`: `UsageCount`, `TotalDiscountGiven = Sum(DiscountAmountActual)`, `RevenueGenerated = Sum(FinalAmount)`.
  4. `RoiPercentage = (RevenueGenerated − TotalDiscountGiven) / TotalDiscountGiven × 100` (nếu `TotalDiscountGiven = 0` thì trả `0` để tránh chia 0).
  5. Sắp xếp giảm dần theo `RevenueGenerated`.

---

*Tham chiếu code đầy đủ: BE tại `src/AutoWashPro.API/Controllers/Admin/AdminReportController.cs` + `src/AutoWash.Application/Services/ReportService.cs` (và các Admin*Controller khác cùng thư mục); FE tại `src/pages/admin/`, `src/components/admin/`, `src/services/admin*Service.js` (repo `smart-automated-car-wash-management-FE`).*

---

# PHẦN 3 — THƯ VIỆN BACKEND SỬ DỤNG (.NET 8)

> Tổng hợp từ các file `.csproj` trong repo (`AutoWash.Application`, `AutoWash.Domain`, `AutoWash.Infrastructure`, `AutoWashPro.API`, `AutoWash.Tests`) — áp dụng chung cho toàn bộ các issue Admin nêu trên.

**Web / API**
- ASP.NET Core Web API (`Microsoft.NET.Sdk.Web`) — MVC Controllers.
  - *Vì sao:* framework chính thức của .NET để build REST API, có sẵn DI container, middleware pipeline, model binding — không cần thêm framework ngoài (như NancyFx, ServiceStack).
- `Microsoft.AspNetCore.OpenApi` + `Swashbuckle.AspNetCore` 10.2.1 — Swagger/OpenAPI docs.
  - *Vì sao:* nhóm nhiều dev (FE/BE tách repo) cần tài liệu API tự sinh, test thử endpoint trực tiếp trên trình duyệt (`/swagger`) mà không cần Postman, đỡ công viết doc tay cho từng issue.
- `Microsoft.AspNetCore.Authentication.JwtBearer` 8.0.10 — xác thực JWT cho toàn bộ Admin API.
  - *Vì sao:* hệ thống có nhiều role (Guest/Member/Admin), cần cơ chế stateless (không lưu session server) để API scale dễ, chuẩn công nghiệp cho SPA/FE gọi qua Bearer token.

**Realtime**
- **SignalR** (`Microsoft.AspNetCore.SignalR`, có sẵn trong shared framework ASP.NET Core, không cần NuGet riêng) — dùng cho `AdminNotificationHub`, `BookingHub` (thông báo booking mới/đổi trạng thái theo thời gian thực, FE-ISSUE-08). Đăng ký tại `Program.cs`: `AddSignalR()`, `MapHub<AdminNotificationHub>`, `MapHub<BookingHub>`.
  - *Vì sao:* yêu cầu nghiệp vụ ISSUE-08 là màn hình Admin phải tự cập nhật khi có booking mới/đổi trạng thái **mà không cần bấm refresh** — nếu dùng polling (FE gọi API lặp lại mỗi vài giây) sẽ tốn tài nguyên và có độ trễ; SignalR đẩy (push) dữ liệu ngay khi có sự kiện, lại là thư viện chính chủ của ASP.NET Core nên không tốn thêm NuGet/hạ tầng (không cần Redis/Kafka pub-sub riêng).

**Database / ORM**
- `Microsoft.EntityFrameworkCore` 8.0.10.
  - *Vì sao:* ORM chính thức của .NET, cho phép viết truy vấn bằng LINQ thay vì SQL thuần, giảm lỗi khi Domain model thay đổi (migrations tự sinh), phù hợp team nhỏ không có DBA chuyên trách viết SQL tay cho mọi query.
- `Npgsql.EntityFrameworkCore.PostgreSQL` 8.0.10 — PostgreSQL provider (đọc view SQL `VwCustomerRfm` cho FE-ISSUE-11).
  - *Vì sao:* PostgreSQL được chọn làm DB chính (free, mã nguồn mở, hỗ trợ tốt kiểu dữ liệu phức tạp và view/window function — cần cho báo cáo RFM ở ISSUE-11); Npgsql là provider EF Core chính thức duy nhất cho Postgres.
- `EFCore.NamingConventions` 8.0.3 — map snake_case (DB) ↔ PascalCase (C#).
  - *Vì sao:* PostgreSQL theo convention đặt tên cột/bảng snake_case, còn C# convention là PascalCase; thư viện này tự động dịch hai chiều để không phải đặt `[Column("ten_cot")]` thủ công trên từng property.
- `Microsoft.EntityFrameworkCore.Design` — hỗ trợ migrations.
  - *Vì sao:* cần thiết để chạy lệnh `dotnet ef migrations add/update` khi cấu hình DB thay đổi (ví dụ thêm cột `IsLocked` cho customer ở ISSUE-09).

**Auth / Bảo mật**
- `BCrypt.Net-Core` 1.6.0 — hash mật khẩu.
  - *Vì sao:* BCrypt có salt tự động và chi phí tính toán (cost factor) cấu hình được, chống brute-force/rainbow table tốt hơn hash thường (MD5/SHA1); là lựa chọn chuẩn OWASP khuyến nghị cho lưu mật khẩu.
- `System.IdentityModel.Tokens.Jwt` + `Microsoft.IdentityModel.Tokens` 8.0.2 — tạo/validate JWT.
  - *Vì sao:* thư viện chính thức của Microsoft để sinh và ký JWT, tương thích trực tiếp với `JwtBearer` middleware ở trên, tránh phải tự viết logic ký/verify token dễ sai sót bảo mật.

**Background jobs** (`AutoNoShowJob`, `PointExpiryJob`, `TierDowngradeJob` — liên quan FE-ISSUE-10/11)
- `BackgroundService`/`IHostedService` built-in của .NET — **không** dùng Hangfire/Quartz.
  - *Vì sao:* các job này chỉ cần chạy định kỳ trong tiến trình (in-process), không cần lưu lịch sử job, retry phức tạp, hay dashboard quản lý job như Hangfire/Quartz đòi hỏi — dùng `BackgroundService` có sẵn trong .NET là đủ, tránh thêm dependency và bảng dữ liệu phụ trợ không cần thiết.

**Testing**
- `xunit` 2.6.6, `xunit.runner.visualstudio` 2.5.4, `Moq` 4.20.70, `Microsoft.EntityFrameworkCore.InMemory` 8.0.10.
  - *Vì sao:* xUnit là framework test phổ biến nhất cho .NET hiện nay (thay thế MSTest cũ); Moq để giả lập (mock) các interface như `IAdminNotifier`, `IBookingHubNotifier` khi test Service mà không cần chạy SignalR/DB thật; EF Core InMemory để test logic Service với DbContext giả, không cần cài PostgreSQL khi chạy CI.

> **Không dùng:** AutoMapper, FluentValidation, MediatR, Serilog — mapping và validate đều viết tay thủ công trong Service layer (đã grep xác nhận không có package/using nào của các thư viện này trong repo).
> *Vì sao không dùng:* dự án quy mô vừa (đồ án SWP391), số lượng DTO/Controller chưa đủ lớn để việc thêm các thư viện này (AutoMapper cho mapping, FluentValidation cho validate, MediatR cho CQRS/mediator pattern, Serilog cho structured logging) mang lại lợi ích rõ rệt so với chi phí học thêm cú pháp và convention riêng của từng thư viện; viết tay giúp code tường minh, dễ debug và dễ giải thích với giảng viên hơn.

---

# PHẦN 4 — BUSINESS RULES LIÊN QUAN THEO TỪNG ISSUE

> Đối chiếu với danh sách đầy đủ BR-01 → BR-66 trong `doc/CONTEXT.md`. Mục đích: khi trình bày issue nào với giảng viên, có thể trích ngay đúng BR làm căn cứ nghiệp vụ.

## FE-ISSUE-08 · Dashboard & Booking Management
- **BR-32** (Maintenance Mode) — trạm/dịch vụ bảo trì không nhận booking mới.
- **BR-33 / BR-33.1** (Notification Dispatch / SignalR Realtime Dispatch) — nền tảng cho việc màn hình Admin tự cập nhật qua `AdminNotificationHub`.
- **BR-40** (Strict Workflow Progression) — Pending → Completed/Failed, hoặc Cancelled/No-show.
- **BR-41** (State Reversibility Constraint) — không sửa ngược trạng thái sau khi đã ở trạng thái cuối.
- **BR-42** (Live Tracking Broadcast) — Admin phải cập nhật trạng thái kịp thời để khách theo dõi.
- **BR-43** (Emergency Stop Log) — nút "Dừng khẩn cấp" phải ghi log ngoại lệ + cảnh báo ngay.
- **BR-44** (License Plate Verification) — đối soát biển số khi check-in, có thể chuyển Cancelled/Failed nếu sai lệch.
- **BR-45 → BR-48** (Payment Methods, Payment Timestamping, Cashier Accountability, Double Payment Protection) — xác nhận thanh toán khi đơn hoàn tất.
- **BR-21** (Real-time Upgrade) — tự nâng hạng ngay khi đơn chuyển Completed.
- **BR-51 → BR-54** (Point Earning Ratio, Earning Trigger, Anti-Fraud Cap, No Points on Cancellation) — cộng điểm thưởng khi đơn Completed.
- **BR-65** (Auto No-Show Trigger) — mốc 15 phút để tự động phát hiện khách không đến.

## FE-ISSUE-09 · Customer Management
- **BR-11** (Data Isolation) — khách chỉ xem/sửa được dữ liệu của chính mình (đối trọng với quyền xem của Admin).
- **BR-12** (Admin Read-Only Data) — Admin xem được lịch sử mọi khách nhưng không được sửa dữ liệu tài chính/điểm của giao dịch đã hoàn tất.
- **BR-13** (Account Locking) — `IsLocked = true` chặn toàn bộ quyền đăng nhập và đặt lịch.
- **BR-66** (Suspension Penalty Logic) — 3 lần No-show trong 30 ngày → tự động khóa đặt lịch 15 ngày (cơ chế khóa tự động, liên quan trực tiếp `IsLocked` ở BR-13).

## FE-ISSUE-10 · Service, Promotion & Tier Config
- **BR-14 → BR-18** (Default Tier, Booking Window theo hạng Member/Silver/Gold/Platinum).
- **BR-21 / BR-21.1** (Real-time Upgrade, Dynamic Effective Tier Resolution).
- **BR-22** (Monthly Review) — cơ sở cho `TierDowngradeJob` hạ hạng theo chi tiêu 12 tháng.
- **BR-23** (Automated Perks) — ưu đãi theo hạng tự tính, không cần khách chọn tay.
- **BR-34 → BR-38** (Vehicle Type Factor, Service Metadata, Gross Pricing VAT, Inactive Services ẩn khỏi FE, Price Lock-in không ảnh hưởng booking cũ khi đổi giá).
- **BR-39** (Invoice Calculation Formula) — `FinalAmount = BaseAmount − TierDiscount − RewardDiscount − PromotionDiscount`, các mức giảm này chính là dữ liệu Admin cấu hình ở issue này.

## FE-ISSUE-11 · Reports (RFM) & Tier Distribution
- **BR-14 → BR-23** (toàn bộ nhóm Hạng thành viên) — cơ sở cho biểu đồ phân bố khách theo hạng.
- **BR-51 → BR-58** (Point Earning Ratio, Earning Trigger, Anti-Fraud Cap, No Points on Cancellation, Point Lifespan, FIFO Deduction, Hard Expiration, Redemption Value) — cơ sở cho khối Loyalty Stats (tổng điểm, điểm sắp hết hạn/đã hết hạn).

## FE-ISSUE-14 · Popular Services Report
- **BR-34 → BR-38** (phân loại dịch vụ, giá, trạng thái active) — chỉ đơn `Completed` mới được gom nhóm theo dịch vụ.
- **BR-36** (Gross Pricing VAT) — ảnh hưởng trực tiếp số liệu doanh thu hiển thị trong báo cáo.

## FE-ISSUE-15 · Peak Occupancy Report
- **BR-24** (Single Vehicle Booking).
- **BR-28** (Time Buffer Per Vehicle — tối thiểu 120 phút giữa 2 lịch cùng biển số).
- **BR-29** (Advance Notice Time — đặt lịch tối thiểu trước 60 phút).
- **BR-30** (Station Buffer Time — nghỉ 5 phút giữa các ca rửa).
- **BR-31** (Station Capacity — 1 trạm chỉ xử lý 1 xe/thời điểm) — liên quan trực tiếp cách tính `MaxParallelSlots`/`OccupancyPercentage` trong report (lưu ý: report hard-code `MaxParallelSlots = 1`, khác với cấu hình `BookingSettings.MaxParallelSlots` dùng lúc tạo booking, hai giá trị không liên thông).

## FE-ISSUE-16 · Promotion ROI Report
- **BR-39** (Invoice Calculation Formula) — nguồn gốc `PromotionDiscount`/`FinalAmount` dùng để tính ROI.
- Gián tiếp liên quan **BR-45 / BR-46** (Payment Methods, Payment Timestamping) vì report chỉ tính đơn có `Transaction.Status == Paid`.

---

⚠️ **Không thuộc phạm vi các issue Admin trên** (thuộc luồng Guest/Member, xem `FLOWS.md`): BR-19/BR-20 (Priority Queue, Timestamp Tie-Breaker), BR-25 → BR-27 (Guest/Member/Daily Quota), BR-63/BR-64 (Free Cancellation Window, State Locking for Edits) — không xuất hiện trực tiếp trong logic Admin Dashboard/Booking/Report nêu trên.
