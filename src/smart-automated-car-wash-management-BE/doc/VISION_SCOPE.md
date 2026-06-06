# VISION_SCOPE

## Business Requirements
- Vận hành bãi gửi xe nhiều tầng chính xác và hiệu quả khi lưu lượng xe ra/vào liên tục.
- Giảm ùn ứ tại cổng, hạn chế sai lệch dữ liệu và thất thoát/khó đối soát doanh thu.
- Tối ưu khả năng sử dụng chỗ đỗ theo loại phương tiện và khu vực.

## Project Scope
- Quản lý thông tin tòa nhà gửi xe, loại phương tiện, phân tầng theo loại xe.
- Quản lý slot đỗ xe và trạng thái slot (trống, đang sử dụng, đặt trước, bảo trì, tạm khóa).
- Quản lý bảng giá và chính sách tính phí gửi xe.
- Ghi nhận lượt gửi xe: thời gian vào/ra, loại xe, cổng vào; hỗ trợ thu phí.
- Hỗ trợ vận hành tại cổng: kiểm tra điều kiện xe vào, nhập/quét biển số, hướng dẫn khu vực gửi.
- Hỗ trợ xử lý ngoại lệ: mất thẻ, sai thông tin xe, quá hạn, gửi sai khu vực, cập nhật trạng thái slot.
- Cung cấp báo cáo lượt xe vào/ra, doanh thu, tỷ lệ lấp đầy, khung giờ cao điểm theo loại xe.
- Cung cấp thông tin cho người gửi xe: thời gian hoạt động, loại xe phục vụ, bảng giá, số slot trống.
- Cho phép người gửi xe theo dõi lượt gửi hiện tại và thanh toán phí.
- Quản lý tài khoản người dùng, phân quyền, và cấu hình hệ thống.

## Limitations
- Các chức năng nâng cao như theo dõi xe chưa thanh toán, sai biển số, quá giờ được nêu là **optional**.
- Đặt chỗ trước chỉ áp dụng **nếu hệ thống hỗ trợ**.
- Tối ưu phân bổ chỗ đỗ bằng AI được **khuyến khích**, không bắt buộc.
