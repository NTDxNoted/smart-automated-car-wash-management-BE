\# Phân tích kỹ thuật: ISSUE-07 (View \& Cancel Booking)



\## 1. Truy vấn Dữ liệu (GET /api/bookings)

\- Cần viết câu lệnh LINQ join bảng `Booking` với bảng `Service`.

\- Đầu vào: `CustomerID` (lấy từ JWT Token).

\- Phân trang: Sử dụng `Skip` và `Take`.



\## 2. Logic Hủy lịch (POST /api/bookings/{id}/cancel)

\- Bước 1: Tìm Booking theo ID.

\- Bước 2: Kiểm tra điều kiện `Status == "Pending"`.

\- Bước 3: Kiểm tra thời gian `ScheduledTime >= DateTime.UtcNow.AddHours(2)`.

\- Bước 4: Đổi trạng thái thành `Cancelled`.

\- Bước 5: Nếu đơn có dùng điểm (kiểm tra `PointsRedeemed > 0`), gọi logic hoàn  trả điểm vào ví LoyaltyAccount.

\- Bước 6: Lưu DB (`SaveChanges`).

