using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoWash.Application.Common;
using AutoWash.Application.Common.Validation;
using AutoWash.Application.DTOs;
using AutoWash.Application.DTOs.Admin;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Entities;
using AutoWash.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoWash.Application.Services
{
    public class AdminBookingService : IAdminBookingService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<AdminBookingService> _logger;
        private readonly IAdminNotifier? _adminNotifier;
        private readonly ITierService? _tierService;
        private readonly IPointService? _pointService;
        private readonly BookingSettings _bookingSettings;

        // adminNotifier/tierService/pointService/bookingSettings có default null để các unit test cũ (chỉ truyền context + logger) không phải sửa.
        public AdminBookingService(IApplicationDbContext context, ILogger<AdminBookingService> logger, IAdminNotifier? adminNotifier = null, ITierService? tierService = null, IPointService? pointService = null, IOptions<BookingSettings>? bookingSettings = null)
        {
            _context = context;
            _logger = logger;
            _adminNotifier = adminNotifier;
            _tierService = tierService;
            _pointService = pointService;
            _bookingSettings = bookingSettings?.Value ?? new BookingSettings();
        }

        public async Task<IEnumerable<AdminBookingListResponse>> GetAllBookingsAsync(string? status, string? date, string? phone, string? plate)
        {
            var query = _context.Bookings.AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BookingStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(b => b.Status == parsedStatus);
            }

            if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var parsedDate))
            {
                query = query.Where(b => b.ScheduledTime.Date == parsedDate.Date);
            }

            if (!string.IsNullOrWhiteSpace(phone))
            {
                query = query.Where(b => b.Phone.Contains(phone));
            }

            if (!string.IsNullOrWhiteSpace(plate))
            {
                query = query.Where(b => b.LicensePlate.Contains(plate));
            }

            var bookings = await query.OrderByDescending(b => b.ScheduledTime).ToListAsync();

            // We need to fetch customer and service names manually or use navigation properties if they existed.
            // Assuming Customer and Service tables are small or we query them.
            var customerIds = bookings.Select(b => b.CustomerID).Distinct().ToList();
            var serviceIds = bookings.Select(b => b.ServiceID).Distinct().ToList();

            var customers = await _context.Customers.Where(c => customerIds.Contains(c.CustomerID)).ToDictionaryAsync(c => c.CustomerID, c => c.FullName);
            var services = await _context.Services.Where(s => serviceIds.Contains(s.ServiceID)).ToDictionaryAsync(s => s.ServiceID, s => s.ServiceName);

            var bookingIds = bookings.Select(b => b.BookingID).ToList();
            var transactions = await _context.Transactions
                .Where(t => bookingIds.Contains(t.BookingID))
                .ToDictionaryAsync(t => t.BookingID, t => t);

            var promotions = await _context.Promotions.ToDictionaryAsync(p => p.PromotionID, p => (p.Title, p.PromoCode));
            var rewardNames = await _context.RewardsCatalog.ToDictionaryAsync(r => r.RewardID, r => r.RewardName);

            return bookings.Select(b =>
            {
                transactions.TryGetValue(b.BookingID, out var transaction);
                return new AdminBookingListResponse
                {
                    BookingID = b.BookingID,
                    CustomerID = b.CustomerID,
                    CustomerName = customers.ContainsKey(b.CustomerID) ? customers[b.CustomerID] : "Guest",
                    Phone = b.Phone,
                    VehicleID = b.VehicleID,
                    LicensePlate = b.LicensePlate,
                    ServiceID = b.ServiceID,
                    ServiceName = services.ContainsKey(b.ServiceID) ? services[b.ServiceID] : "Unknown Service",
                    ScheduledTime = b.ScheduledTime,
                    CheckInTime = b.CheckInTime,
                    Status = b.Status.ToString(),
                    TotalPrice = b.FinalAmount,
                    BaseAmount = b.BaseAmount,
                    DiscountApplied = b.DiscountApplied,
                    FinalAmount = b.FinalAmount,
                    PointsEarned = b.PointsEarned,
                    PaymentStatus = transaction?.Status.ToString(),
                    PaymentMethod = transaction?.PaymentMethod.ToString(),
                    PaymentAt = transaction?.PaidAt,
                    CreatedAt = b.CreatedAt,
                    IsWalkIn = b.IsWalkIn,
                    CompletedAt = b.CompletedAt,
                    InvoiceCode = transaction != null ? BookingDisplayHelper.FormatInvoiceCode(transaction.TransactionID) : null,
                    PromotionApplied = BookingDisplayHelper.GetPromotionApplied(b.PromotionID, b.RewardID, promotions, rewardNames)
                };
            });
        }

        public async Task<AdminBookingListResponse> GetBookingByIdAsync(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) throw new Exception("BOOKING_NOT_FOUND");

            var customer = await _context.Customers.FindAsync(booking.CustomerID);
            var service = await _context.Services.FindAsync(booking.ServiceID);
            var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.BookingID == booking.BookingID);

            var promotions = await _context.Promotions.ToDictionaryAsync(p => p.PromotionID, p => (p.Title, p.PromoCode));
            var rewardNames = await _context.RewardsCatalog.ToDictionaryAsync(r => r.RewardID, r => r.RewardName);

            return new AdminBookingListResponse
            {
                BookingID = booking.BookingID,
                CustomerID = booking.CustomerID,
                CustomerName = customer?.FullName ?? "Guest",
                Phone = booking.Phone,
                VehicleID = booking.VehicleID,
                LicensePlate = booking.LicensePlate,
                ServiceID = booking.ServiceID,
                ServiceName = service?.ServiceName ?? "Unknown Service",
                ScheduledTime = booking.ScheduledTime,
                CheckInTime = booking.CheckInTime,
                Status = booking.Status.ToString(),
                TotalPrice = booking.FinalAmount,
                BaseAmount = booking.BaseAmount,
                DiscountApplied = booking.DiscountApplied,
                FinalAmount = booking.FinalAmount,
                PointsEarned = booking.PointsEarned,
                PaymentStatus = transaction?.Status.ToString(),
                PaymentMethod = transaction?.PaymentMethod.ToString(),
                PaymentAt = transaction?.PaidAt,
                CreatedAt = booking.CreatedAt,
                IsWalkIn = booking.IsWalkIn,
                CompletedAt = booking.CompletedAt,
                InvoiceCode = transaction != null ? BookingDisplayHelper.FormatInvoiceCode(transaction.TransactionID) : null,
                PromotionApplied = BookingDisplayHelper.GetPromotionApplied(booking.PromotionID, booking.RewardID, promotions, rewardNames)
            };
        }

        public async Task<AdminBookingListResponse> CreateWalkInBookingAsync(CreateWalkInBookingRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CustomerName))
                throw new Exception("CUSTOMER_NAME_REQUIRED: Vui lòng nhập tên khách hàng.");

            if (string.IsNullOrWhiteSpace(request.Phone))
                throw new Exception("PHONE_REQUIRED: Vui lòng nhập số điện thoại.");

            if (string.IsNullOrWhiteSpace(request.Plate))
                throw new Exception("LICENSE_PLATE_REQUIRED: Vui lòng nhập biển số xe.");

            if (!LicensePlateValidator.IsValid(request.Plate))
                throw new Exception("INVALID_LICENSE_PLATE: Biển số xe không đúng định dạng chuẩn Việt Nam (VD: 30F-123.45, 51F12345).");

            if (request.ServiceId <= 0)
                throw new Exception("SERVICE_REQUIRED: Vui lòng chọn dịch vụ.");

            if (!DateTime.TryParse(request.BookingDate, out var datePart))
                throw new Exception("INVALID_DATETIME: Ngày đặt lịch không hợp lệ.");

            if (!TimeSpan.TryParse(request.BookingTime, out var timePart))
                throw new Exception("INVALID_DATETIME: Giờ đặt lịch không hợp lệ.");

            // BookingDate/BookingTime là giờ địa phương VN (giống format trả về ở GetAvailableSlotsAsync);
            // trừ 7h để lưu thống nhất dạng UTC như mọi ScheduledTime khác trong hệ thống.
            var scheduledTime = datePart.Date.Add(timePart).AddHours(-7);

            var service = await _context.Services.FirstOrDefaultAsync(s => s.ServiceID == request.ServiceId);
            if (service == null)
                throw new Exception("SERVICE_NOT_FOUND: Không tìm thấy dịch vụ.");

            if (service.Price <= 0)
                throw new Exception("SERVICE_PRICE_NOT_CONFIGURED: Dịch vụ này chưa được cấu hình đơn giá.");

            // scheduledTime đã ở dạng UTC (Kind=Unspecified, xem comment phía trên) — chuẩn hoá tường minh
            // để so sánh nhất quán với ScheduledTime đọc từ DB (Npgsql luôn trả Unspecified cho cột
            // timestamp dù giá trị lưu là UTC).
            var newStartUtc = DateTime.SpecifyKind(scheduledTime, DateTimeKind.Utc);

            var minCheckUtc = newStartUtc.AddHours(-12);
            var maxCheckUtc = newStartUtc.AddHours(12);

            var activeBookings = await _context.Bookings
                .Where(b => b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.Failed && b.Status != BookingStatus.NoShow)
                .Where(b => b.ScheduledTime >= minCheckUtc && b.ScheduledTime <= maxCheckUtc)
                .Select(b => b.ScheduledTime)
                .ToListAsync();

            // Chỉ trừ đúng slot đã chọn — không tính thời lượng dịch vụ tràn qua slot kế tiếp.
            int overlapCount = 0;
            foreach (var bScheduledTime in activeBookings)
            {
                var bStartUtc = bScheduledTime.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(bScheduledTime, DateTimeKind.Utc)
                    : bScheduledTime.ToUniversalTime();

                if (bStartUtc == newStartUtc)
                {
                    overlapCount++;
                }
            }

            int maxParallelSlots = _bookingSettings.MaxParallelSlots > 0 ? _bookingSettings.MaxParallelSlots : 1;
            if (overlapCount >= maxParallelSlots)
                throw new Exception("SLOT_NOT_AVAILABLE: Khung giờ này đã đầy (đạt giới hạn số lượng xe rửa song song).");

            var phone = request.Phone.Trim();
            var plate = request.Plate.Trim().ToUpperInvariant();

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == phone);
            if (customer == null)
            {
                customer = new Customer
                {
                    FullName = request.CustomerName.Trim(),
                    Phone = phone,
                    Password = "WALKIN",
                    Role = "GUEST",
                    TierID = 1,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.CustomerID == customer.CustomerID && v.LicensePlate == plate);

            if (vehicle == null)
            {
                vehicle = new Vehicle
                {
                    CustomerID = customer.CustomerID,
                    LicensePlate = plate,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Vehicles.Add(vehicle);
                await _context.SaveChangesAsync();
            }

            var booking = new Booking
            {
                CustomerID = customer.CustomerID,
                Phone = phone,
                VehicleID = vehicle.VehicleID,
                LicensePlate = plate,
                ServiceID = service.ServiceID,
                ScheduledTime = scheduledTime,
                Status = BookingStatus.Pending,
                BaseAmount = service.Price,
                DiscountApplied = 0,
                FinalAmount = service.Price,
                // Khách vãng lai không tích điểm (không có tài khoản/loyalty tier thật) — xem thêm
                // PointService.EarnPointsAsync, nơi việc cộng điểm cũng bị chặn cho các booking này.
                PointsEarned = 0,
                IsWalkIn = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Walk-in booking created BookingID={BookingID}, CustomerID={CustomerID}, Plate={Plate}",
                booking.BookingID, customer.CustomerID, plate);

            if (_adminNotifier != null)
            {
                await _adminNotifier.NotifyNewBookingAsync(booking.BookingID, customer.FullName, plate, service.ServiceName);
            }

            return new AdminBookingListResponse
            {
                BookingID = booking.BookingID,
                CustomerID = customer.CustomerID,
                CustomerName = customer.FullName,
                Phone = booking.Phone,
                VehicleID = booking.VehicleID,
                LicensePlate = booking.LicensePlate,
                ServiceID = booking.ServiceID,
                ServiceName = service.ServiceName,
                ScheduledTime = booking.ScheduledTime,
                CheckInTime = booking.CheckInTime,
                Status = booking.Status.ToString(),
                TotalPrice = booking.FinalAmount,
                BaseAmount = booking.BaseAmount,
                DiscountApplied = booking.DiscountApplied,
                FinalAmount = booking.FinalAmount,
                PointsEarned = booking.PointsEarned,
                CreatedAt = booking.CreatedAt,
                IsWalkIn = booking.IsWalkIn
            };
        }

        public async Task<object> UpdateBookingStatusAsync(int id, UpdateBookingStatusRequest request)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) throw new Exception("BOOKING_NOT_FOUND");

            if (booking.Status != BookingStatus.Pending)
            {
                throw new InvalidOperationException($"INVALID_STATUS_TRANSITION: Không thể chuyển từ {booking.Status} sang {request.NewStatus}");
            }

            if (!Enum.TryParse<BookingStatus>(request.NewStatus, true, out var newStatus))
            {
                throw new ArgumentException("Trạng thái không hợp lệ.");
            }

            // Admin có thể đánh dấu No-show thủ công bất kỳ lúc nào trong lúc đơn còn chờ check-in
            // (kể cả trước khi hết 15 phút, ví dụ khách gọi báo không đến) — chỉ chặn khi khách đã check-in.
            // Việc tự động chuyển No-show sau 15 phút do AutoNoShowJob xử lý ở background.
            if (newStatus == BookingStatus.NoShow && booking.CheckInTime != null)
            {
                throw new InvalidOperationException("INVALID_STATUS_TRANSITION: Khách đã check-in, không thể đánh dấu No-show.");
            }

            var previousStatus = booking.Status;
            booking.Status = newStatus;

            if (newStatus == BookingStatus.Completed || newStatus == BookingStatus.Failed || newStatus == BookingStatus.Cancelled || newStatus == BookingStatus.NoShow)
            {
                booking.CompletedAt = DateTime.UtcNow;
            }

            if (newStatus == BookingStatus.NoShow)
            {
                await ApplyNoShowPenaltyAsync(booking);
            }

            await _context.SaveChangesAsync();

            if (newStatus == BookingStatus.Completed && booking.CustomerID > 0)
            {
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerID == booking.CustomerID);
                if (customer != null)
                {
                    customer.TotalSpending += booking.FinalAmount;
                    await _context.SaveChangesAsync();

                    if (_tierService != null)
                    {
                        await _tierService.EvaluateUpgradeAsync(customer.CustomerID);
                    }

                    if (_pointService != null)
                    {
                        await _pointService.EarnPointsAsync(booking.BookingID);
                    }
                }
            }

            _logger.LogInformation("Booking {BookingID} status updated from {OldStatus} to {NewStatus}", id, previousStatus, newStatus);

            // BR-33: Kích hoạt Notification (Mô phỏng qua Log) ngay sau khi thay đổi trạng thái
            _logger.LogInformation("NOTIFICATION TRIGGERED (BR-33): Đã gửi tin nhắn đến SĐT {Phone} - Trạng thái đơn đặt lịch của bạn đã được cập nhật thành: {NewStatus}.", booking.Phone, newStatus);

            // Đẩy real-time cho Admin đang online, thay cho FE phải polling để phát hiện đổi trạng thái.
            if (_adminNotifier != null)
            {
                var customerName = booking.CustomerID > 0
                    ? (await _context.Customers.FindAsync(booking.CustomerID))?.FullName ?? booking.Phone
                    : booking.Phone;
                await _adminNotifier.NotifyBookingStatusChangedAsync(id, customerName, booking.LicensePlate, previousStatus.ToString(), newStatus.ToString());
            }

            return new
            {
                bookingId = booking.BookingID,
                previousStatus = previousStatus.ToString(),
                newStatus = booking.Status.ToString(),
                updatedAt = DateTime.UtcNow
            };
        }

        public async Task<object> UpdateLicensePlateAsync(int id, UpdateLicensePlateRequest request)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) throw new Exception("BOOKING_NOT_FOUND");

            if (booking.Status != BookingStatus.Pending)
            {
                throw new InvalidOperationException("Chỉ có thể sửa biển số khi đơn hàng ở trạng thái Pending.");
            }

            booking.LicensePlate = request.NewLicensePlate;
            await _context.SaveChangesAsync();

            return new { bookingId = booking.BookingID, licensePlate = booking.LicensePlate, message = "Cập nhật biển số thành công" };
        }

        public async Task<object> CheckInAsync(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) throw new Exception("BOOKING_NOT_FOUND");

            if (booking.Status != BookingStatus.Pending)
            {
                throw new InvalidOperationException("Chỉ có thể check-in khi đơn hàng ở trạng thái Pending.");
            }

            booking.CheckInTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new { bookingId = booking.BookingID, checkInTime = booking.CheckInTime, message = "Check-in thành công" };
        }

        private async Task ApplyNoShowPenaltyAsync(Booking booking)
        {
            if (booking.CustomerID <= 0 && string.IsNullOrWhiteSpace(booking.Phone))
            {
                return;
            }

            // c.Phone != "GUEST" loại trừ tài khoản khách vãng lai dùng chung (BookingService gán mọi
            // booking guest vào 1 CustomerID) — nếu không, no-show của các khách lạ khác nhau sẽ bị
            // cộng dồn và khóa nhầm tài khoản hệ thống dùng chung.
            var customer = await _context.Customers.FirstOrDefaultAsync(c =>
                (booking.CustomerID > 0 && c.CustomerID == booking.CustomerID && c.Phone != "GUEST") ||
                (!string.IsNullOrWhiteSpace(booking.Phone) && c.Phone == booking.Phone));

            if (customer == null)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var cutoff = now.AddDays(-30);
            var noShowCount = 1 + await _context.Bookings.CountAsync(b =>
                b.Status == BookingStatus.NoShow &&
                ((b.CompletedAt ?? b.CreatedAt) >= cutoff) &&
                ((b.CustomerID > 0 && b.CustomerID == customer.CustomerID) ||
                 (!string.IsNullOrWhiteSpace(b.Phone) && b.Phone == customer.Phone)));

            if (noShowCount >= 3)
            {
                customer.IsLocked = true;
                customer.SuspendedUntil = now.AddDays(15);
                _logger.LogWarning(
                    "Customer {CustomerID} suspended for {NoShowCount} no-shows within 30 days.",
                    customer.CustomerID,
                    noShowCount);
            }
        }

        public async Task<object> EmergencyStopAsync(int id, EmergencyStopRequest request)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) throw new Exception("BOOKING_NOT_FOUND");

            if (booking.CheckInTime == null)
            {
                throw new InvalidOperationException("BOOKING_NOT_STARTED: Không thể dừng khẩn cấp đơn chưa bắt đầu rửa xe (chưa check-in).");
            }

            if (booking.Status != BookingStatus.Pending)
            {
                throw new InvalidOperationException("INVALID_STATUS: Đơn hàng đã kết thúc (hoàn thành, thất bại hoặc hủy).");
            }

            _logger.LogError("EMERGENCY STOP TRIGGERED for Booking {BookingID}. Reason: {Reason}", id, request.Reason);

            booking.Status = BookingStatus.Failed;
            booking.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            if (_adminNotifier != null)
            {
                var customer = booking.CustomerID > 0 ? await _context.Customers.FindAsync(booking.CustomerID) : null;
                await _adminNotifier.NotifyBookingStatusChangedAsync(id, customer?.FullName ?? booking.Phone, booking.LicensePlate, "Pending", "Failed");
            }

            return new
            {
                bookingId = booking.BookingID,
                loggedAt = DateTime.UtcNow,
                message = "Sự cố đã được ghi nhận và gửi cảnh báo tới Admin"
            };
        }
    }
}
