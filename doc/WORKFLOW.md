# AutoWash Pro - Workflow Catalogue

> Tài liệu workflow cho 27 screen của AutoWash Pro.
>
> `Num of Testcases` là số lượng test case baseline đề xuất cho QA. Đây là kế hoạch kiểm thử theo workflow, không phải số test case đã được triển khai trong repository.
> Ngoài ra, con số này cũng được hiểu là số bước kiểm thử thực hiện cho từng workflow, tức là số thao tác/kiểm tra QA cần chạy để xác minh luồng nghiệp vụ.
>
> Các workflow dưới đây được xây dựng dựa trên các chức năng thật của dự án: đăng nhập, khách hàng, đặt lịch, điểm thưởng, quản trị và báo cáo.

## 1. Workflow Summary

|  No | Workflow Code | Workflow Name                    | Description                                                                                                      | Num of Testcases |
| --: | ------------- | -------------------------------- | ---------------------------------------------------------------------------------------------------------------- | ---------------: |
|   1 | WF-001        | Đăng nhập Member                 | Đăng nhập bằng số điện thoại và mật khẩu; xác thực thông tin, kiểm tra tài khoản bị khóa và phát hành JWT.       |                8 |
|   2 | WF-002        | Trang chủ khách hàng             | Hiển thị dashboard tổng quan gồm hạng thành viên, tổng điểm, dịch vụ nổi bật và lối tắt tới các chức năng chính. |                6 |
|   3 | WF-003        | Hồ sơ cá nhân                    | Thành viên xem và cập nhật họ tên, số điện thoại không đổi, hạng hiện tại và tổng chi tiêu.                      |                7 |
|   4 | WF-004        | Quản lý xe của tôi               | Xem danh sách xe; thêm, sửa, xóa biển số xe với xác thực OTP và kiểm tra định dạng biển số.                      |               10 |
|   5 | WF-005        | Danh sách dịch vụ                | Lấy và hiển thị các dịch vụ đang hoạt động, phân biệt giá theo loại xe Car/Bike.                                 |                6 |
|   6 | WF-006        | Đặt lịch rửa xe                  | Chọn xe hoặc nhập biển số, chọn dịch vụ và khung giờ còn trống để tạo booking.                                   |               14 |
|   7 | WF-007        | Áp dụng ưu đãi khi đặt lịch      | Kiểm tra mã promotion và áp dụng điểm/phần thưởng trong phiên đặt lịch.                                          |               12 |
|   8 | WF-008        | Xác nhận và hóa đơn đặt lịch     | Tính và hiển thị BaseAmount, TierDiscount, RewardDiscount, PromotionDiscount và FinalAmount trước khi xác nhận.  |                9 |
|   9 | WF-009        | Lịch sử đặt lịch                 | Lấy danh sách booking của chính khách hàng, phân trang và lọc theo trạng thái.                                   |                6 |
|  10 | WF-010        | Chi tiết đơn đặt lịch            | Xem chi tiết booking, theo dõi trạng thái realtime và hủy đơn khi còn đủ điều kiện.                              |               10 |
|  11 | WF-011        | Ví điểm thưởng                   | Hiển thị điểm khả dụng, điểm đang khóa, điểm sắp hết hạn và lịch sử cộng/trừ điểm.                               |                8 |
|  12 | WF-012        | Danh sách phần thưởng đổi điểm   | Hiển thị catalog reward đang hoạt động và điều kiện đổi điểm.                                                    |                5 |
|  13 | WF-013        | Thông báo                        | Hiển thị thông báo về booking, thanh toán, trạng thái xử lý và sự kiện tài khoản.                                |                5 |
|  14 | WF-014        | Tài khoản bị khóa hoặc tạm ngưng | Hiển thị lý do tài khoản bị khóa, thời gian mở lại và chặn đăng nhập/đặt lịch tương ứng.                         |                6 |
|  15 | WF-015        | Đăng nhập Admin                  | Admin đăng nhập bằng thông tin riêng, nhận JWT có role ADMIN và được chuyển tới dashboard quản trị.              |                7 |
|  16 | WF-016        | Admin Dashboard                  | Hiển thị KPI và thông báo booking/khách mới theo thời gian thực qua SignalR.                                     |                9 |
|  17 | WF-017        | Quản lý khách hàng               | Admin tìm kiếm, xem chi tiết và khóa/mở khóa tài khoản Member.                                                   |                9 |
|  18 | WF-018        | Quản lý đặt lịch                 | Admin xem, lọc booking theo trạng thái, ngày, số điện thoại và biển số.                                          |                8 |
|  19 | WF-019        | Chi tiết và xử lý đơn đặt lịch   | Admin check-in, đối soát biển số, cập nhật trạng thái và ghi nhận dừng khẩn cấp.                                 |               14 |
|  20 | WF-020        | Xác nhận thanh toán              | Admin chọn Cash/Transfer, xác nhận đã thu tiền, ghi PaymentTimestamp và ngăn thanh toán hai lần.                 |                9 |
|  21 | WF-021        | Quản lý dịch vụ                  | Admin CRUD dịch vụ, cấu hình giá/thời lượng/loại xe và bật tắt trạng thái hoạt động.                             |               12 |
|  22 | WF-022        | Quản lý khuyến mãi               | Admin CRUD promotion, cấu hình điều kiện, bật/tắt và xem usage.                                                  |               12 |
|  23 | WF-023        | Quản lý phần thưởng              | Admin CRUD reward, cấu hình giảm phần trăm/giảm cố định và bật/tắt reward.                                       |               10 |
|  24 | WF-024        | Quản lý hạng thành viên          | Admin xem và cập nhật ngưỡng MinSpending của từng tier.                                                          |                7 |
|  25 | WF-025        | Báo cáo tổng quan                | Tổng hợp doanh thu, số booking và KPI theo khoảng thời gian.                                                     |                8 |
|  26 | WF-026        | Báo cáo chi tiết                 | Hiển thị popular services, RFM, tier distribution, occupancy peak và promotion ROI.                              |               12 |
|  27 | WF-027        | Báo cáo Loyalty                  | Tổng hợp tích điểm, đổi điểm, điểm hết hạn và số liệu loyalty toàn hệ thống.                                     |                8 |

**Tổng baseline:** 239 test cases.

## 2. Detailed Workflows

### WF-001 - Đăng nhập Member

**Actor:** Member  
**Precondition:** Tài khoản đã đăng ký và chưa bị khóa.  
**Entry point:** `/login`  
**API:** `POST /api/auth/login`

1. Member nhập SĐT và mật khẩu.
2. Frontend kiểm tra SĐT gồm 10 chữ số bắt đầu bằng `0` và mật khẩu tối thiểu 6 ký tự.
3. Frontend thử luồng đăng nhập Admin trước; nếu không thành công thì chuyển sang luồng Member.
4. Backend tìm Customer theo SĐT.
5. Backend kiểm tra `IsLocked` và xác thực mật khẩu bằng BCrypt.
6. Backend resolve tier, tạo JWT và trả thông tin Member.
7. Frontend lưu `member_token` và thông tin Member vào local storage.
8. Hệ thống chuyển Member tới `/bookings`.

**Failure paths:**

- Thiếu hoặc sai định dạng dữ liệu: `VALIDATION_FAILED`.
- Không tìm thấy SĐT hoặc sai mật khẩu: `INVALID_CREDENTIALS`.
- `IsLocked = true`: `ACCOUNT_LOCKED`, HTTP 403.
- Không kết nối được API: hiển thị lỗi kết nối máy chủ.

### WF-002 - Trang chủ khách hàng

**Actor:** Member  
**Precondition:** Có JWT hợp lệ.

1. Frontend đọc thông tin Member từ AuthContext.
2. Hệ thống lấy profile và loyalty summary.
3. Hệ thống tính effective tier từ tier và tổng chi tiêu nếu cần.
4. Hiển thị hạng, điểm, dịch vụ nổi bật và các nút đặt lịch/lịch sử.
5. Khi token hết hạn, chuyển Member về `/login`.

### WF-003 - Hồ sơ cá nhân

**Actor:** Member  
**API chính:** `GET/PATCH /api/profile`

1. Member mở trang profile.
2. Hệ thống lấy profile theo `CustomerID` trong JWT.
3. Hiển thị họ tên, SĐT, tier và tổng chi tiêu.
4. Member cập nhật trường được phép sửa.
5. Backend chỉ cập nhật dữ liệu của chính Member và trả profile mới.
6. Frontend cập nhật AuthContext/local storage.

### WF-004 - Quản lý xe của tôi

**Actor:** Member  
**API chính:** `/api/vehicles`

1. Member mở danh sách xe của chính mình.
2. Member thêm, sửa hoặc xóa biển số.
3. Frontend chuẩn hóa biển số và kiểm tra định dạng Việt Nam.
4. Khi thêm/sửa, hệ thống gửi yêu cầu OTP tới SĐT Member.
5. Member nhập OTP.
6. Backend xác thực OTP rồi thực hiện thay đổi.
7. Hệ thống từ chối OTP sai/hết hạn, biển số không hợp lệ hoặc thao tác trên xe không thuộc Member.

### WF-005 - Danh sách dịch vụ

**Actor:** Guest hoặc Member  
**API:** `GET /api/services`

1. Người dùng mở danh sách dịch vụ.
2. Hệ thống chỉ trả dịch vụ `IsActive = true`.
3. Người dùng chọn loại xe Car/Bike.
4. Hệ thống hiển thị giá cuối đã bao gồm VAT, mô tả và thời lượng.
5. Dịch vụ bảo trì/ngừng hoạt động không được chọn để đặt lịch.

### WF-006 - Đặt lịch rửa xe

**Actor:** Guest hoặc Member  
**API:** `POST /api/bookings`

1. Người dùng chọn xe đã lưu hoặc nhập biển số.
2. Hệ thống tải dịch vụ và slot khả dụng.
3. Người dùng chọn dịch vụ, ngày và giờ.
4. Backend kiểm tra trạng thái dịch vụ/trạm, thời gian báo trước, capacity, buffer 5 phút và xung đột biển số 120 phút.
5. Backend kiểm tra quota: Guest tối đa 1 Pending, Member tối đa 3 Pending và tối đa 2 booking chưa hoàn thành trong ngày.
6. Hệ thống áp dụng thứ tự ưu tiên tier khi slot cuối bị tranh chấp.
7. Hệ thống tạo booking ở trạng thái Pending.
8. Hệ thống phát thông báo trạng thái và trả thông tin booking.

### WF-007 - Áp dụng ưu đãi khi đặt lịch

1. Người dùng nhập mã promotion.
2. Backend kiểm tra mã tồn tại, thời hạn, trạng thái, quota usage và điều kiện áp dụng.
3. Người dùng chọn reward hoặc số điểm muốn sử dụng.
4. Backend kiểm tra Member, số điểm tối thiểu 50 và giới hạn điểm tối đa 50% hóa đơn.
5. Điểm được hold trong phiên booking.
6. Promotion/reward không hợp lệ thì hệ thống giữ nguyên giá trị trước ưu đãi và trả lý do lỗi.

### WF-008 - Xác nhận và hóa đơn đặt lịch

1. Hệ thống nhận BaseAmount từ dịch vụ đã chọn.
2. Tính giảm theo tier.
3. Tính giảm theo reward.
4. Tính giảm theo promotion.
5. Tính `FinalAmount = BaseAmount - TierDiscount - RewardDiscount - PromotionDiscount`.
6. Hiển thị chi tiết hóa đơn trước thao tác xác nhận.
7. Khi xác nhận, lưu snapshot giá vào booking để thay đổi giá dịch vụ sau này không ảnh hưởng booking cũ.
8. Nếu tạo booking thất bại, hoàn lại điểm đang hold.

### WF-009 - Lịch sử đặt lịch

1. Member mở lịch sử booking.
2. Backend lấy booking theo CustomerID từ JWT.
3. Frontend hiển thị phân trang.
4. Member lọc Pending, Completed, Failed, Cancelled hoặc No-show.
5. Không cho phép Member xem dữ liệu của tài khoản khác.

### WF-010 - Chi tiết đơn đặt lịch

1. Member chọn một booking thuộc tài khoản của mình.
2. Hệ thống hiển thị dịch vụ, xe, thời gian, giá và trạng thái.
3. Hệ thống nhận cập nhật trạng thái realtime nếu có.
4. Member chọn hủy khi booking là Pending và còn ít nhất 2 giờ trước giờ hẹn.
5. Backend chuyển trạng thái sang Cancelled và hoàn điểm đã hold nếu có.
6. Không cho sửa/hủy booking đã ở trạng thái cuối.

### WF-011 - Ví điểm thưởng

1. Member mở ví điểm.
2. Hệ thống hiển thị điểm khả dụng, điểm hold và điểm sắp hết hạn.
3. Hệ thống hiển thị lịch sử earning, redemption, expiry và refund.
4. Điểm chỉ được cộng sau khi booking Completed và thanh toán đủ.
5. Điểm earning tối đa 500 điểm mỗi booking.
6. Job định kỳ trừ điểm hết hạn sau 12 tháng.
7. Khi đổi điểm, hệ thống trừ theo FIFO.

### WF-012 - Danh sách phần thưởng đổi điểm

1. Member mở catalog reward.
2. Hệ thống lấy reward đang hoạt động.
3. Hiển thị loại giảm phần trăm hoặc giảm cố định, điều kiện và số điểm cần thiết.
4. Reward không hoạt động hoặc không đủ điểm không cho áp dụng.

### WF-013 - Thông báo

1. Người dùng mở danh sách thông báo.
2. Hệ thống hiển thị thông báo theo tài khoản và thời gian.
3. Booking thay đổi trạng thái tạo thông báo.
4. Admin nhận notification realtime qua SignalR khi có booking mới/khách mới.
5. Lỗi kết nối realtime không làm mất dữ liệu thông báo đã lưu.

### WF-014 - Tài khoản bị khóa hoặc tạm ngưng

1. Member cố đăng nhập bằng tài khoản bị khóa.
2. Backend trả `ACCOUNT_LOCKED` và HTTP 403.
3. Frontend hiển thị lý do/liên hệ Admin.
4. Nếu tài khoản bị phạt No-show, hệ thống hiển thị thời gian `SuspendedUntil`.
5. Trong thời gian phạt, hệ thống chặn đặt lịch online.
6. Sau thời gian phạt, quyền đặt lịch được mở lại theo rule.

### WF-015 - Đăng nhập Admin

1. Admin nhập SĐT và mật khẩu tại `/login`.
2. Frontend gọi luồng Admin trước luồng Member.
3. Backend xác thực thông tin Admin và role.
4. Backend trả JWT Admin.
5. Frontend lưu `admin_token`, `admin_user`.
6. Hệ thống chuyển tới `/admin/dashboard`.
7. Request tới endpoint Admin không có role hợp lệ bị từ chối.

### WF-016 - Admin Dashboard

1. Admin mở dashboard.
2. Hệ thống tải KPI tổng quan.
3. Hệ thống kết nối `AdminNotificationHub` bằng JWT.
4. Khi có booking mới hoặc khách mới, SignalR đẩy sự kiện lên dashboard.
5. Dashboard cập nhật badge, danh sách và biểu đồ mà không cần polling.
6. Nếu mất kết nối, client xử lý reconnect hoặc hiển thị trạng thái offline.

### WF-017 - Quản lý khách hàng

1. Admin mở danh sách khách hàng.
2. Hệ thống tải danh sách và cho phép tìm theo tên/SĐT.
3. Admin mở chi tiết một Member.
4. Hệ thống hiển thị profile, tier, booking và loyalty summary.
5. Admin khóa hoặc mở khóa tài khoản.
6. Member bị khóa không thể đăng nhập và đặt lịch.
7. Admin không được sửa dữ liệu tài chính/điểm của giao dịch đã hoàn tất.

### WF-018 - Quản lý đặt lịch

1. Admin mở booking management.
2. Hệ thống tải danh sách booking.
3. Admin lọc theo trạng thái, ngày, SĐT hoặc biển số.
4. Admin mở chi tiết booking.
5. Hệ thống trả kết quả ổn định khi không có dữ liệu hoặc tham số lọc không hợp lệ.
6. Endpoint yêu cầu role Admin.

### WF-019 - Chi tiết và xử lý đơn đặt lịch

1. Admin mở chi tiết booking Pending.
2. Admin check-in xe.
3. Admin đối soát biển số thực tế.
4. Nếu sai, Admin sửa biển số hoặc chuyển Failed.
5. Admin cập nhật trạng thái theo chuỗi hợp lệ.
6. Hệ thống chặn cập nhật ngược hoặc sửa booking trạng thái cuối.
7. Khi có lỗi vận hành/dừng khẩn cấp, hệ thống ghi log và phát cảnh báo.
8. Trạng thái mới được broadcast tới client liên quan.

### WF-020 - Xác nhận thanh toán

1. Admin mở booking đủ điều kiện thanh toán.
2. Admin chọn Cash hoặc Transfer.
3. Với Cash, Admin xác nhận đã thu đủ tiền.
4. Backend ghi PaymentTimestamp.
5. Hệ thống chặn thanh toán lần hai cho cùng booking.
6. Chỉ booking thanh toán đủ mới được chuyển Completed.
7. Khi Completed, hệ thống kích hoạt cộng điểm và cập nhật tier.

### WF-021 - Quản lý dịch vụ

1. Admin xem danh sách dịch vụ.
2. Admin tạo dịch vụ với tên, giá, mô tả, hóa chất, thời lượng và loại xe.
3. Backend validate dữ liệu bắt buộc và giá không âm.
4. Admin sửa thông tin dịch vụ.
5. Admin bật/tắt dịch vụ hoặc chuyển bảo trì.
6. Dịch vụ inactive không xuất hiện ở màn hình khách hàng.
7. Giá mới không thay đổi snapshot của booking cũ.

### WF-022 - Quản lý khuyến mãi

1. Admin xem danh sách promotion.
2. Admin tạo hoặc cập nhật mã, thời hạn, giá trị giảm và điều kiện.
3. Backend kiểm tra trùng mã và dữ liệu thời gian.
4. Admin bật/tắt promotion.
5. Người dùng chỉ áp dụng được promotion đang active và còn hiệu lực.
6. Hệ thống cập nhật usage sau khi booking hợp lệ.
7. Admin xem usage theo promotion.

### WF-023 - Quản lý phần thưởng

1. Admin xem catalog reward.
2. Admin tạo reward giảm phần trăm hoặc giảm cố định.
3. Backend validate giá trị reward và số điểm yêu cầu.
4. Admin sửa hoặc tắt reward.
5. Reward inactive không hiển thị cho Member.
6. Khi áp dụng, hệ thống tính đúng giá trị giảm và giới hạn điểm theo hóa đơn.

### WF-024 - Quản lý hạng thành viên

1. Admin xem các tier Member, Silver, Gold và Platinum.
2. Hệ thống hiển thị MinSpending của từng tier.
3. Admin cập nhật ngưỡng hợp lệ.
4. Hệ thống áp dụng tier cao nhất đạt điều kiện.
5. Job định kỳ xét hạ tier dựa trên tổng chi tiêu 12 tháng gần nhất.
6. Thay đổi cấu hình không làm mất lịch sử booking/điểm.

### WF-025 - Báo cáo tổng quan

1. Admin chọn khoảng thời gian báo cáo.
2. Hệ thống tổng hợp doanh thu và số booking.
3. Hệ thống trả KPI chính theo khoảng thời gian.
4. Booking Cancelled/Failed không được tính sai vào doanh thu Completed.
5. Dashboard hiển thị trạng thái loading, empty và error.

### WF-026 - Báo cáo chi tiết

1. Admin mở báo cáo chi tiết.
2. Hệ thống tải popular services.
3. Hệ thống tải RFM customer analysis.
4. Hệ thống tải tier distribution.
5. Hệ thống tải peak occupancy theo giờ/ngày.
6. Hệ thống tải promotion ROI.
7. Bộ lọc thời gian được áp dụng nhất quán cho các báo cáo.
8. Dữ liệu rỗng vẫn trả cấu trúc biểu đồ hợp lệ.

### WF-027 - Báo cáo Loyalty

1. Admin mở báo cáo Loyalty.
2. Hệ thống tổng hợp điểm đã cộng, đã đổi, đã hết hạn và đã hoàn trả.
3. Hệ thống phân tích theo thời gian/tier nếu được chọn.
4. Số liệu phải đối soát với transaction history.
5. Điểm bị hold không được tính là điểm khả dụng.
6. Admin không được sửa trực tiếp số liệu giao dịch từ màn hình báo cáo.

## 3. Cross-workflow Rules

- **Authentication:** API riêng tư phải nhận JWT hợp lệ; dữ liệu Member được giới hạn theo `CustomerID` trong token.
- **Authorization:** Endpoint Admin yêu cầu role `ADMIN`; Member không được truy cập dữ liệu quản trị.
- **Booking state:** `Pending -> Completed`, `Pending -> Failed`, `Pending -> Cancelled` hoặc `Pending -> No-show`; trạng thái cuối không được quay ngược.
- **Pricing:** `FinalAmount = BaseAmount - TierDiscount - RewardDiscount - PromotionDiscount`.
- **Loyalty:** Chỉ cộng điểm sau Completed và thanh toán đủ; điểm đổi dùng FIFO; điểm hết hạn sau 12 tháng.
- **Realtime:** SignalR dùng cho thông báo Admin và cập nhật booking; REST API vẫn là nguồn dữ liệu chính.
- **Security:** Không đưa password, JWT secret hoặc database password vào log, tài liệu công khai hay response API.

## 4. Traceability

| Workflow group                                    | Business rules chính                               |
| ------------------------------------------------- | -------------------------------------------------- |
| WF-001, WF-003, WF-004, WF-014, WF-015            | BR-02, BR-03, BR-06, BR-10, BR-11, BR-13           |
| WF-002, WF-024                                    | BR-14 đến BR-23                                    |
| WF-005, WF-006, WF-007, WF-008                    | BR-24 đến BR-39                                    |
| WF-009, WF-010, WF-013, WF-019                    | BR-40 đến BR-44, BR-63 đến BR-66                   |
| WF-020                                            | BR-45 đến BR-48                                    |
| WF-011, WF-012, WF-023, WF-027                    | BR-51 đến BR-62                                    |
| WF-016 đến WF-018, WF-021, WF-022, WF-025, WF-026 | Admin authorization, operations và reporting rules |
