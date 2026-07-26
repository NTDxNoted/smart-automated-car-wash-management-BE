# KỊCH BẢN KIỂM THỬ HỆ THỐNG END-TO-END (BLACK-BOX TESTING)
## LUỒNG NGHIỆP VỤ CHÍNH: ĐẶT LỊCH & THANH TOÁN (BOOKING & PAYMENT LIFECYCLE)

Tài liệu này thiết lập các kịch bản kiểm thầm hệ thống (System Testing) theo phương pháp **Hộp đen (Black-box Testing)** trong môi trường giả lập luồng đi thực tế từ đầu đến cuối (E2E) của ứng dụng AutoWash Pro.

---

### TC01: Đặt lịch rửa xe thành công và tích lũy điểm thành viên (Luồng chuẩn - Happy Path)
* **Pre-conditions (Điều kiện tiên quyết):**
  * Khách hàng đã có tài khoản thành viên đang hoạt động trên hệ thống (Hạng: Member).
  * Khách hàng đã đăng nhập thành công vào giao diện Customer.
  * Trong cơ sở dữ liệu có sẵn ít nhất một gói dịch vụ Rửa xe cơ bản (Basic Wash - 150.000đ).
  * Tiệm rửa xe còn trống lịch vào khung giờ 09:00 - 10:00 ngày mai.
* **Test Steps (Các bước thực hiện):**
  1. Khách hàng truy cập vào trang Đặt lịch (`/booking`).
  2. Tại **Bước 1: Chọn dịch vụ**, khách hàng chọn gói "Rửa xe cơ bản" (Basic Wash) có giá hiển thị là 150.000đ và bấm "Tiếp tục".
  3. Tại **Bước 2: Thông tin xe & Thời gian**, khách hàng nhập biển số xe (Ví dụ: `51F-999.99`), chọn ngày đặt lịch là Ngày mai, chọn khung giờ trống `09:00 - 10:00`.
  4. Khách hàng kiểm tra tóm tắt thông tin hóa đơn (Tổng tiền gốc: 150.000đ, Giảm giá: 0đ, Số tiền cuối: 150.000đ) và bấm "Xác nhận đặt lịch".
* **Expected Result (Kết quả kỳ vọng):**
  * Hệ thống hiển thị thông báo đặt lịch thành công.
  * Màn hình chuyển hướng về trang Lịch sử đặt lịch (`/booking-history`).
  * Một bản ghi đặt lịch mới được tạo trong database với trạng thái ban đầu là `Pending`.
  * Số tiền ghi nhận trên giao diện là: Tổng tiền `150.000đ`, Điểm tích lũy dự kiến: `15 điểm` (tương ứng 10% giá trị hóa đơn cho hạng Member).

---

### TC02: Đặt lịch rửa xe có áp dụng mã Khuyến mãi (Promotion) & giảm giá thành công
* **Pre-conditions (Điều kiện tiên quyết):**
  * Khách hàng đã đăng nhập tài khoản.
  * Trong hệ thống có mã khuyến mãi đang hoạt động tên là **`CHAOHE2026`** (Giảm 20.000đ cho hóa đơn từ 100.000đ trở lên).
  * Tài khoản đã đăng ký xe có biển số `51H-123.45` trong hồ sơ.
* **Test Steps (Các bước thực hiện):**
  1. Khách hàng vào trang Đặt lịch, chọn dịch vụ "Rửa xe cao cấp" (Premium Wash - 250.000đ).
  2. Tại bước chọn xe, chọn biển số `51H-123.45` từ danh sách xe đã lưu. Chọn lịch hẹn lúc 15:00 hôm nay.
  3. Tại ô nhập mã khuyến mãi, nhập chữ **`CHAOHE2026`** và nhấn "Áp dụng".
  4. Xác nhận thông tin hóa đơn và nhấn nút "Đặt ngay".
* **Expected Result (Kết quả kỳ vọng):**
  * Mã khuyến mãi được áp dụng thành công trên giao diện.
  * Phần hiển thị tóm tắt tiền thay đổi tức thì:
    * Giá gốc (Base Amount): `250.000đ`
    * Giảm giá áp dụng (Discount Applied): `20.000đ`
    * Số tiền phải trả (Final Amount): `230.000đ`
  * Bản ghi lịch hẹn lưu vào database có ID của mã khuyến mãi tương ứng (`PromotionID`) và trường `FinalAmount` lưu đúng giá trị `230000.00`.

---

### TC03: Đổi Điểm Tích Lũy lấy Voucher giảm giá và sử dụng khi Đặt Lịch
* **Pre-conditions (Điều kiện tiên quyết):**
  * Tài khoản thành viên đang có **`500 điểm`** tích lũy trong ví loyalty.
  * Trong danh mục quà tặng (`Rewards Catalog`) có gói voucher trị giá giảm **`50.000đ`** cần đổi bằng **`300 điểm`**.
* **Test Steps (Các bước thực hiện):**
  1. Khách hàng truy cập trang Tích điểm (`/loyalty`).
  2. Tại thẻ phần thưởng giảm giá 50.000đ (yêu cầu 300 điểm), khách hàng bấm nút **"Dùng ngay"**.
  3. Hệ thống chuyển hướng khách hàng sang trang Đặt lịch. Khách hàng tiến hành chọn dịch vụ "Rửa xe chi tiết" (Detailing - 500.000đ).
  4. Hệ thống tự động áp dụng Voucher 50.000đ vừa đổi vào phần giảm giá của đơn đặt lịch này. Khách hàng tiến hành đặt lịch.
* **Expected Result (Kết quả kỳ vọng):**
  * Số điểm của khách hàng lập tức bị trừ 300 điểm (còn lại 200 điểm hiển thị trên trang cá nhân).
  * Hóa đơn thanh toán của lịch đặt xe được giảm trực tiếp 50.000đ:
    * Giá gốc: `500.000đ`
    * Giảm giá Voucher: `50.000đ`
    * Tổng thanh toán cuối cùng: `450.000đ`
  * Trạng thái ví điểm ghi nhận một giao dịch trừ điểm dạng `Redeem` trong lịch sử điểm thành viên.

---

### TC04: Quy trình Check-in, Thực hiện rửa xe và Thanh toán tại quầy (Luồng Staff/Admin)
* **Pre-conditions (Điều kiện tiên quyết):**
  * Đã có một lịch hẹn rửa xe trong hệ thống với trạng thái `Pending` (Số tiền thanh toán: 150.000đ).
  * Nhân viên (Role: ADMIN hoặc STAFF) đã đăng nhập vào trang quản trị hệ thống.
* **Test Steps (Các bước thực hiện):**
  1. Khách hàng lái xe đến tiệm rửa xe. Nhân viên vào danh sách quản lý lịch hẹn (`Admin Booking List`).
  2. Nhân viên tìm lịch hẹn theo biển số xe hoặc số điện thoại của khách và bấm nút **"Check-in"** để ghi nhận xe đã vào tiệm.
  3. Sau khi xe rửa xong, nhân viên mở chi tiết lịch hẹn và bấm nút **"Thanh toán" (Payment)**.
  4. Hệ thống hiển thị bảng chọn phương thức thanh toán. Nhân viên chọn phương thức **"Chuyển khoản" (Transfer - Quét QR)** và hướng dẫn khách hàng quét mã QR hiển thị trên màn hình.
  5. Sau khi nhận được tiền chuyển khoản, nhân viên bấm nút **"Xác nhận đã nhận tiền" (Confirm Paid)**.
* **Expected Result (Kết quả kỳ vọng):**
  * Khi bấm Check-in: Trạng thái lịch hẹn đổi sang hiển thị thời gian check-in thực tế (`CheckInTime = NOW()`).
  * Khi bấm Xác nhận thanh toán thành công:
    * Trạng thái đơn đặt lịch chuyển thành **`Completed`**.
    * Trạng thái hóa đơn thanh toán chuyển thành **`Paid`** tại bảng `Transaction`.
    * Hệ thống Backend tự động cộng điểm thưởng vào ví Loyalty của khách hàng dựa trên hệ số nhân hạng thành viên (`PointsEarned` được tính và cập nhật vào `LoyaltyAccount`).

---

### TC05: Ngoại lệ - Ngăn chặn đặt lịch khi tài khoản bị khóa do vi phạm chính sách "No-Show"
* **Pre-conditions (Điều kiện tiên quyết):**
  * Khách hàng đã có 3 lần đặt lịch nhưng không đến tiệm và không hủy lịch trong vòng 30 ngày gần nhất (Trạng thái lịch hẹn tự động chuyển sang `NoShow`).
  * Hệ thống Job tự động quét và khóa tài khoản của khách hàng này (`IsLocked = true` và `SuspendedUntil` đặt thời gian khóa 15 ngày).
* **Test Steps (Các bước thực hiện):**
  1. Khách hàng bị khóa tài khoản cố tình đăng nhập vào trang web và tiến hành đặt lịch rửa xe mới.
  2. Khách hàng thực hiện chọn dịch vụ, chọn giờ và nhấn nút gửi yêu cầu đặt lịch.
* **Expected Result (Kết quả kỳ vọng):**
  * **Tại Frontend:** Khi nhấn đặt lịch, hệ thống hiển thị thông báo lỗi cảnh báo màu đỏ: *"Tài khoản của bạn tạm thời bị khóa đặt lịch đến ngày DD/MM/YYYY do vi phạm chính sách không đến đúng hẹn quá 3 lần."*
  * **Tại Backend:** API `/api/booking` kiểm tra điều kiện tài khoản khách hàng, phát hiện `IsLocked == true` và trả về mã lỗi HTTP `400 Bad Request` kèm chuỗi thông báo từ chối tạo lịch hẹn mới, đảm bảo bảo mật dữ liệu tuyệt đối.
