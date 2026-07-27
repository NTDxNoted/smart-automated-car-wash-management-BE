# BẢNG DANH SÁCH TOÀN DIỆN: 66 BUSINESS RULES (AUTO_WASH PRO)


## I. Quản lý Tài khoản & Định danh (Account & Identity)
### BR-01 (Guest Mode): Hệ thống cho phép người dùng đặt lịch vãng lai (Guest Booking) mà không cần đăng ký tài khoản.  
### BR-02 (Member Mode): Người dùng muốn tích điểm, thăng hạng và đổi phần thưởng bắt buộc phải đăng ký tài khoản Thành viên (Member).  
### BR-03 (Unique Identity): Mỗi tài khoản Member được định danh duy nhất thông qua Số điện thoại (SĐT này đóng vai trò là Username khi đăng nhập).  
### BR-04 (Registration Fields): Thông tin bắt buộc khi đăng ký Member bao gồm: Họ và tên, Số điện thoại, Mật khẩu và Xác nhận mật khẩu.  
### BR-05 (Duplication Validation): Hệ thống phải kiểm tra và từ chối đăng ký nếu Số điện thoại đã tồn tại trong cơ sở dữ liệu.  
### BR-06 (Password Security): Mật khẩu của Member phải được mã hóa bằng thuật toán Bcrypt trước khi lưu vào Database.  
### BR-07 (Vehicle Profile): Khách hàng vãng lai (Guest) không được lưu trữ thông tin xe vào hệ thống.  
### BR-08 (Unlimited Vehicles): Khách hàng thành viên (Member) được phép lưu trữ không giới hạn số lượng xe vào hồ sơ cá nhân để chọn nhanh khi đặt lịch, hoặc tự nhập biển số xe mới trực tiếp tại màn hình đặt lịch.
### BR-08.1 (Vietnamese License Plate Uniqueness): Mọi biển số xe phải tuân thủ định dạng biển số xe Việt Nam (VD: 30F-123.45, 51A12345). Do quy chuẩn cấu trúc biển số xe tại Việt Nam độc lập tuyệt đối giữa Ô tô và Xe máy (không bao giờ trùng lặp), hệ thống tự động định danh phương tiện qua Biển số xe mà không cần phân loại thủ công.  
### BR-09 (Vehicle Decoupling): Một biển số xe có thể được lưu trữ trong danh sách xe của nhiều tài khoản Member khác nhau (ví dụ: các thành viên trong gia đình dùng chung xe).  
### BR-10 (OTP Verification): Khi Member thực hiện thêm xe mới hoặc thay đổi biển số xe trong Profile, hệ thống yêu cầu xác thực qua mã OTP gửi về SĐT (Mô phỏng logic ứng dụng).  
### BR-11 (Data Isolation): Khách hàng chỉ có quyền xem, chỉnh sửa thông tin hồ sơ, ví điểm và lịch sử đặt lịch của chính mình.  
### BR-12 (Admin Read-Only Data): Admin có quyền xem lịch sử của tất cả khách hàng nhưng tuyệt đối không được chỉnh sửa thông tin tài chính hoặc điểm số của các giao dịch đã hoàn tất.  
### BR-13 (Account Locking): Tài khoản Member bị Admin chuyển sang trạng thái khóa (IsLocked = true) sẽ bị chặn toàn bộ quyền đăng nhập và đặt lịch.  I
## I. Hạng Thành viên & Quyền ưu tiên (Membership Tiers & Priority)
### BR-14 (Default Tier): Tài khoản Member mới đăng ký thành công sẽ mặc định ở hạng thấp nhất là Member.  
### BR-15 (Member Booking Window): Hạng vãng lai (Guest) và hạng Member thông thường chỉ được đặt lịch trước tối đa 07 ngày.  
### BR-16 (Silver Booking Window): Hạng Silver được đặt lịch trước tối đa 10 ngày.  
### BR-17 (Gold Booking Window): Hạng Gold được đặt lịch trước tối đa 12 ngày.  
### BR-18 (Platinum Booking Window): Hạng Platinum được đặt lịch trước tối đa 14 ngày.  
### BR-19 (Priority Queue Logic): Khi một slot thời gian cuối cùng của trạm có nhiều người cùng gửi yêu cầu đặt lịch, hệ thống tự động xếp hàng đợi ưu tiên dựa trên cấp bậc: Platinum > Gold > Silver > Member > Guest (không phụ thuộc vào thời gian bấm đặt).  
### BR-20 (Timestamp Tie-Breaker): Nếu hai khách hàng có cùng hạng thành viên (hoặc cùng là Guest) đặt trùng slot cuối cùng, hệ thống sẽ ưu tiên người có thời gian bấm xác nhận (Timestamp) trước.
### BR-21 (Real-time Upgrade): Hệ thống tự động quét và nâng hạng ngay lập tức (Real-time) khi tổng chi tiêu (TotalSpending) của Member đạt mốc quy định ngay sau khi đơn hàng chuyển thành Completed.  
### BR-21.1 (Dynamic Effective Tier Resolution): Giao diện người dùng (Frontend) và API tự động tính toán Hạng thực tế (Effective Tier) dựa trên mốc TotalSpending (Silver >= 500k, Gold >= 1.5M, Platinum >= 3M) ngay khi đăng nhập hoặc mở ứng dụng, đảm bảo hiển thị đúng Hạng cao nhất của khách hàng mà không phụ thuộc vào giá trị seed tĩnh trong Database.  
### BR-22 (Monthly Monthly Review): Hệ thống chạy lệnh tự động quét vào lúc 00:00 ngày đầu tiên mỗi tháng để xét hạ hạng dựa trên tổng chi tiêu của 12 tháng gần nhất.  
### BR-23 (Automated Perks): Các đặc quyền về giá, giảm giá cố định theo hạng phải được hệ thống tự động tính toán và áp dụng tại màn hình đặt lịch mà không cần khách hàng chọn bằng tay.  II
## I. Hạn mức & Ràng buộc Đặt lịch (Booking Quota & Constraints)
### BR-24 (Single Vehicle Booking): Mỗi một lượt đặt lịch (Booking Slot) chỉ được áp dụng cho một biển số xe duy nhất.  
### BR-25 (Guest Quota): Một số điện thoại vãng lai (Guest) chỉ được phép có tối đa 01 lịch hẹn ở trạng thái Pending trên hệ thống.  
### BR-26 (Member Quota): Một tài khoản Member được phép có tối đa 03 lịch hẹn ở trạng thái Pending tại một thời điểm (để phục vụ nhu cầu rửa nhiều xe của gia đình hoặc công ty).
### BR-27 (Daily Limit): Một tài khoản (cả Guest và Member) không được phép có quá 02 lịch hẹn ở trạng thái "Chưa hoàn thành" trong cùng một ngày.  
### BR-28 (Time Buffer Per Vehicle): Cùng một biển số xe không được phép có 2 lịch hẹn cách nhau dưới 120 phút để tránh việc đặt trùng hoặc giữ chỗ ảo.  
### BR-29 (Advance Notice Time): Thời gian đặt lịch trực tuyến tối thiểu phải trước 60 phút (1 tiếng) so với giờ hẹn.  
### BR-30 (Station Buffer Time): Hệ thống tự động chèn khoảng nghỉ 5 phút sau mỗi ca rửa xe để nhân viên vệ sinh trạm và chuẩn bị dụng cụ.  
### BR-31 (Station Capacity): Một trạm rửa xe tại một thời điểm chỉ xử lý duy nhất 01 xe; lịch hẹn chỉ được chuyển sang Pending hoặc Confirmed nếu khung giờ đó còn trống.  
### BR-32 (Maintenance Mode): Trạm rửa xe hoặc dịch vụ được Admin chuyển sang trạng thái "Bảo trì" hoặc "Tạm khóa" sẽ không tiếp nhận bất kỳ lịch đặt mới nào.  
### BR-33 (Notification Dispatch): Hệ thống phải tự động kích hoạt tính năng gửi thông báo (Mô phỏng qua log hệ thống/UI) ngay sau khi đơn đặt lịch thay đổi trạng thái thành công.  
### BR-33.1 (SignalR Realtime Dispatch): Hệ thống tích hợp SignalR WebSocket Hub (AdminNotificationHub) để tự động đẩy thông báo thời gian thực đến Admin Dashboard ngay khi có đơn đặt lịch mới hoặc có khách hàng mới đăng ký.  
## I
## V. Dịch vụ, Giá cả & Thuế phí (Services & Pricing)
### BR-34 (Vehicle Type Factor): Phân loại phương tiện (Ô tô - Car hoặc Xe máy - Bike) là yếu tố quyết định để hệ thống áp dụng bảng giá dịch vụ tương ứng.  
### BR-35 (Service Metadata): Mỗi dịch vụ trong danh mục bắt buộc phải cấu hình đầy đủ: Tên dịch vụ, Giá tiền, Mô tả chi tiết các bước, Loại hóa chất sử dụng và Thời gian thực hiện ước tính.  
### BR-36 (Gross Pricing): Tất cả giá dịch vụ hiển thị trên giao diện dành cho khách hàng phải là giá cuối cùng đã bao gồm thuế giá trị gia tăng VAT.  
### BR-37 (Inactive Services): Dịch vụ ở trạng thái "Ngừng hoạt động" (IsActive = false) phải bị ẩn khỏi màn hình đặt lịch của khách hàng.  
### BR-38 (Price Lock-in): Admin có quyền cập nhật giá dịch vụ, nhưng mức giá mới này tuyệt đối không được làm ảnh hưởng đến số tiền của các booking đã được xác nhận trước đó.  
### BR-39 (Invoice Calculation Formulas): Tổng số tiền hóa đơn được tính theo công thức:$$\text{FinalAmount} = \text{BaseAmount} - \text{TierDiscount} - \text{RewardDiscount} - \text{PromotionDiscount}$$  
## V. Vận hành & Trạng thái Ngoại lệ (Operations & Live Workflow)
### BR-40 (Strict Workflow Progression): Tiến trình trạng thái của một đơn đặt lịch bắt buộc phải tuân thủ nghiêm ngặt theo chuỗi: Pending (Chờ xác nhận) $\rightarrow$ Completed (Hoàn thành) hoặc Failed (Thất bại). Đơn đang ở trạng thái Pending có thể bị chuyển thành Cancelled (Đã hủy) hoặc No-show (Vắng mặt) theo các quy tắc tương ứng.  
### BR-41 (State Reversibility Constraint): Trạng thái đơn hàng không được phép cập nhật ngược hoặc thay đổi sau khi đã đạt trạng thái cuối (Completed, Failed, Cancelled, No-show).
### BR-42 (Live Tracking Broadcast): Trạng thái đơn hàng phải được Admin cập nhật kịp thời để khách hàng có thể theo dõi tiến độ thực tế tại quầy.  
### BR-43 (Emergency Stop Log): Mọi quy trình vận hành gặp lỗi kỹ thuật hoặc nhân viên nhấn nút "Dừng khẩn cấp" tại quầy phải được hệ thống ghi log ngoại lệ và gửi cảnh báo ngay lập tức về màn hình của Admin.  
### BR-44 (License Plate Verification): Khi xe đến trạm, Admin/Nhân viên phải đối soát biển số thực tế. Nếu phát hiện sai lệch so với lịch đặt hoặc phát sinh sự cố, Admin có quyền chuyển đơn hàng từ Pending sang Cancelled (Đã hủy) hoặc Failed (Thất bại).  
## I. Quy trình Thanh toán Offline tại quầy (Offline Payment Rules)
### BR-45 (Payment Methods Available): Hệ thống chỉ hỗ trợ ghi nhận hai hình thức thanh toán trực tiếp tại quầy: Tiền mặt (Cash) hoặc Chuyển khoản ngân hàng thủ công (Transfer).  
### BR-46 (Payment Timestamping): Hệ thống bắt buộc phải tự động ghi lại chính xác thời gian (PaymentTimestamp) ngay khi Admin bấm xác nhận thanh toán thành công.  
### BR-47 (Cashier Accountability): Đối với hình thức Tiền mặt, nhân viên tại quầy bắt buộc phải bấm nút xác nhận "Đã thu đủ tiền mặt" thì hệ thống mới cho phép chuyển trạng thái đơn hàng sang Completed.  
### BR-48 (Double Payment Protection): Hệ thống phải chặn không cho phép bấm thanh toán hoặc xác nhận thu tiền 2 lần cho cùng một mã đơn hàng.  
## I. Cơ chế Tích điểm & Đổi thưởng (Loyalty Engine Rules)
### BR-51 (Point Earning Ratio): Tỷ lệ tích điểm mặc định là 10,000 VNĐ chi tiêu = 1 điểm thưởng (Giá trị này Admin có thể thay đổi trong phần cấu hình hệ thống).  
### BR-52 (Earning Trigger): Điểm tích lũy chỉ được tự động cộng vào tài khoản Member sau khi đơn hàng chuyển hẳn sang trạng thái Completed và đã được xác nhận thanh toán đủ.  
### BR-53 (Anti-Fraud Cap): Một giao dịch đơn hàng không được tích quá 500 điểm thưởng nhằm ngăn chặn các hành vi gian lận cố tình đẩy giá đơn hàng.  
### BR-54 (No Points on Cancellation): Tuyệt đối không cộng điểm thưởng cho các lịch đặt bị hủy (Cancelled) hoặc lịch đặt thất bại (Failed).  
### BR-55 (Point Lifespan): Điểm thưởng có thời hạn sử dụng chính xác là 12 tháng kể từ ngày giao dịch phát sinh đơn hàng đó thành công.  
### BR-56 (FIFO Deduction): Khi Member thực hiện đổi điểm lấy quà, hệ thống phải áp dụng thuật toán FIFO (First In, First Out): Điểm nào tích lũy trước, cũ hơn sẽ được ưu tiên trừ trước.  
### BR-57 (Hard Expiration): Điểm sau khi quá hạn 12 tháng sẽ tự động bị khấu trừ về 0 và hệ thống không cho phép khôi phục lại dưới bất kỳ lý do gì.  
### BR-58 (Point Redemption Value): Giá trị quy đổi khi dùng điểm: 1 điểm = 1,000 VNĐ giảm trực tiếp vào số tiền phải trả của hóa đơn.  
### BR-58.1 (Percentage and Fixed Reward Calculation): Hệ thống hỗ trợ 2 hình thức đổi phần thưởng: Giảm theo phần trăm (%) và Giảm số tiền cố định (VNĐ). Khi phần thưởng là dạng Phần trăm (VD: Giảm 10%), giá trị giảm thực tế được tính bằng: BaseAmount * (DiscountValue / 100) và hiển thị trực tiếp số tiền được giảm cho khách hàng.  
### BR-59 (Redemption Threshold): Khách hàng phải tích lũy tối thiểu từ 50 điểm trở lên trong ví mới kích hoạt được tính năng đổi phần thưởng.  
### BR-60 (Redemption Cap): Điểm thưởng chỉ được dùng để thanh toán tối đa 50% tổng giá trị của hóa đơn đơn hàng đó.  
### BR-61 (Non-Cash Convertibility): Điểm thưởng tuyệt đối không được quy đổi thành tiền mặt hoặc thối lại tiền thừa cho khách.  
### BR-62 (Point Locking & Return Logic): Khi Member bấm đặt lịch và áp dụng đổi điểm lấy phần thưởng, số điểm đó sẽ chuyển sang trạng thái Đóng băng (Hold/Lock). Nếu đơn hàng bị chuyển thành Cancelled hoặc Failed, hệ thống phải hoàn trả lại số điểm này về tài khoản Member và giữ nguyên ngày hết hạn gốc của chúng.VII
## I. Hủy lịch & Xử phạt Vắng mặt (Cancellation & No-Show Penalties)
### BR-63 (Free Cancellation Window): Khách hàng chỉ được phép tự hủy lịch miễn phí trên giao diện Web khi đơn hàng đang ở trạng thái Pending và thời điểm hủy phải cách giờ hẹn ít nhất 02 tiếng.  
### BR-64 (State Locking for Edits): Đơn hàng đã chuyển sang trạng thái Completed, Failed hoặc Cancelled thì toàn bộ dữ liệu liên quan sẽ bị khóa cứng, không thể chỉnh sửa hay cập nhật.  
### BR-65 (Auto No-Show Trigger): Nếu xe không đến check-in tại trạm quá 15 phút kể từ giờ hẹn trên lịch, hệ thống tự động chuyển trạng thái đơn hàng sang No-show.  
### BR-66 (Suspension Penalty Logic): Bất kỳ tài khoản nào (định danh qua SĐT) tích lũy đủ 03 lần No-show trong vòng 30 ngày sẽ bị hệ thống tự động khóa chức năng đặt lịch trực tuyến trong 15 ngày tiếp theo. Sau khi hết thời gian phạt, quyền đặt lịch sẽ tự động được mở lại



# PROJECT CONTEXT: AutoWash Pro (Full Specification)

## SWP391 - Smart Automated Car Wash Management System

---

# 1. TECH STACK

- **Frontend:** React (Vite) + Tailwind CSS + Axios
- **Backend:** C# (.NET 8.0 Web API) + Entity Framework Core
- **API Documentation:** Swagger UI (Standardized DTOs & RESTful)
- **Database:** PostgreSQL (Supabase)

---

# 2. CORE STRATEGY: GUEST VS. MEMBER

## Guest (Vãng lai)
- Đặt nhanh qua SĐT + Biển số
- Giới hạn 01 đơn Pending
- Không tích điểm

## Member (Thành viên)
- Có tài khoản
- Giới hạn 03 đơn Pending
- Tích điểm, nâng hạng (Tier), đổi thưởng
- Lưu xe không giới hạn

---

# 3. BUSINESS RULES (BR) - THE 66 RULES SOURCE

## I. Tài khoản & Xe (BR-01 -> BR-13)

- Hỗ trợ cả Guest và Member
- Member định danh qua SĐT
- Mật khẩu mã hóa Bcrypt
- Member lưu xe không giới hạn
- Một xe có thể thuộc nhiều tài khoản; điểm tính cho người đặt lịch
- Admin có quyền khóa tài khoản (`IsLocked`)

---

## II. Hạng Thành viên & Ưu tiên (BR-14 -> BR-23)

### Các hạng thành viên
- Member
- Silver
- Gold
- Platinum

### Booking Window
- Member: 7 ngày
- Silver: 10 ngày
- Gold: 12 ngày
- Platinum: 14 ngày

### Ưu tiên slot cuối
`Platinum > Gold > Silver > Member > Guest`

### Automation
- Tự động nâng hạng (Real-time)
- Tự động hạ hạng vào mùng 1 hàng tháng

---

## III. Đặt lịch & Ràng buộc (BR-24 -> BR-33)

- Guest: tối đa 1 đơn Pending
- Member: tối đa 3 đơn Pending
- Cùng biển số phải cách nhau tối thiểu 120 phút
- Đặt lịch trước tối thiểu 60 phút
- Tự động chèn 5 phút nghỉ giữa các ca
- Trạm bảo trì sẽ chặn đặt lịch
- Gửi thông báo tự động khi đổi trạng thái

---

## IV. Dịch vụ & Giá cả (BR-34 -> BR-39)

- Phân loại Bike/Car để áp giá
- Giá hiển thị là giá cuối (Gross Price)

### Công thức thanh toán

```text
Final = Base - TierDiscount - RewardDiscount - Promotion
```

---

## V. Vận hành & Trạng thái (BR-40 -> BR-44)

### Workflow
```text
Pending → Completed / Failed / Cancelled / No-show
```

- Không được cập nhật ngược trạng thái
- Admin đối soát biển số thực tế khi xe đến

---

## VI. Thanh toán Offline (BR-45 -> BR-48)

- Chỉ hỗ trợ:
  - Cash
  - Transfer

- Admin bắt buộc nhấn “Đã thu tiền”
- Không hỗ trợ Refund (ngoài phạm vi dự án)

---

## VII. Loyalty & Điểm thưởng (BR-51 -> BR-62)

### Quy tắc tích điểm
- 10.000 VNĐ = 1 điểm
- Tối đa 500 điểm / đơn

### Quy tắc hết hạn
- Điểm hết hạn sau 12 tháng
- Áp dụng FIFO

### Quy tắc đổi thưởng
- 1 điểm = 1.000 VNĐ
- Tối thiểu 50 điểm để đổi
- Tối đa 50% hóa đơn

### Point Lock
- Đóng băng điểm khi đặt lịch
- Hoàn lại nếu đơn bị Cancel/Failed

---

## VIII. Hủy lịch & No-Show (BR-63 -> BR-66)

- Hủy miễn phí trước 2 tiếng (chỉ Pending)
- Quá 15 phút không đến = No-Show
- 3 lần No-show trong 30 ngày:
  - Khóa đặt lịch online 15 ngày

---

## IX. Bảo mật & Quản lý Phiên làm việc (Session Security & Expiration) (BR-67 -> BR-70)

### BR-67 (Single Active Session - Last Login Wins)
Mỗi tài khoản Customer hoặc Admin tại một thời điểm chỉ tồn tại tối đa **01 phiên đăng nhập hợp lệ**. Đăng nhập ở thiết bị mới sẽ tự động vô hiệu hóa phiên làm việc của thiết bị cũ ở lượt gọi API tiếp theo.

### BR-68 (JWT Session Expiration Time)
Thời gian hiệu lực tối đa của mỗi phiên làm việc (JWT Token) là **90 phút**. Sau 90 phút kể từ thời điểm đăng nhập, Token sẽ tự động hết hạn, Frontend sẽ xóa Storage và yêu cầu người dùng đăng nhập lại.

### BR-69 (Clean Logout Session Revocation)
Khi người dùng hoặc Admin bấm Đăng xuất (Logout) chủ động, hệ thống gán `ActiveSessionId = null` trong Database để thu hồi quyền truy cập của Token lập tức.

### BR-70 (Unrestricted Login Bypass)
Các đường dẫn API Đăng nhập & Đăng ký (`/api/auth/login`, `/api/auth/register`, `/api/admin/auth/login`) được ưu tiên bỏ qua kiểm tra Token cũ tại Middleware để người dùng luôn có thể đăng nhập lại thành công bất kể trạng thái Token hiện tại.
