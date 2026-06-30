## ISSUE-09: [PAYMENT] Cash Payment Processing

**Mô tả**
Admin ghi nhận thanh toán bằng tiền mặt (Cash) trực tiếp tại quầy. Hệ thống tự động ghi nhận server timestamp khi xác nhận thành công. Chặn double payment nhờ Unique Index trên Database. Sau khi thanh toán thành công, tự động thực hiện chuỗi nghiệp vụ: tích điểm thưởng, tích lũy TotalSpending, cập nhật LastVisit và tự động nâng hạng Tier (nếu đủ điều kiện).

**Tasklist**
- [ ] POST /api/admin/bookings/{id}/payment — tạo giao dịch thanh toán tiền mặt
- [ ] Kiểm tra trạng thái Booking phải là Pending, nếu thuộc Final State (Completed/Failed/Cancelled/Noshow) -> Trả về lỗi 400 Bad Request.
- [ ] Kiểm tra body request bắt buộc phải có { "confirmed": true } (Xác nhận Admin đã cầm tiền mặt), nếu false -> 400.
- [ ] Lưu bản ghi vào bảng Transaction với PaymentMethod = 'Cash', PaidAt = NOW(), Status = 'Paid'.
- [ ] Bắt lỗi DbUpdateException để check trùng index idx_txn_booking_paid nhằm chặn double payment -> 409 Conflict.
- [ ] Xử lý hậu thanh toán (Bọc tất cả trong IDbContextTransaction):
  - [ ] Cập nhật trạng thái Booking -> Completed, ghi nhận CompletedAt.
  - [ ] Tính điểm thưởng: PointsEarned = min(floor(FinalAmount / 10000), 500).
  - [ ] Cộng PointsEarned vào LoyaltyAccount.TotalPoints và tạo 1 bản ghi PointTransaction loại Earn.
  - [ ] Cộng FinalAmount vào Customer.TotalSpending và gán Customer.LastVisit = NOW().
  - [ ] Gọi dịch vụ ITierService để tự động kiểm tra nâng hạng real-time (BR-21).

**Endpoints**
```
POST /api/admin/bookings/{id}/payment
GET  /api/admin/bookings/{id}/transaction
```

**Lưu ý (Sửa đổi theo thực tế)**
- BR-45 & BR-47: Chỉ xử lý Tiền mặt (Cash) tại quầy và yêu cầu cam kết từ nhân viên (Confirmed = true).
- Bỏ qua logic điểm Tạm khóa (Held): Vì hệ thống chưa lưu trạng thái Held điểm lúc đặt lịch, tại bước này hệ thống chỉ xử lý tích điểm thưởng mới (Earn) dựa trên hóa đơn. Điểm đổi thưởng (Redeem) nếu khách có áp dụng lúc đặt lịch thì đã được trừ thẳng vào số tiền FinalAmount thông qua Invoice Service từ trước.
