using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoWash.Application.DTOs.Admin;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Entities;
using AutoWash.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutoWash.Application.Services
{
    public class AdminBookingService : IAdminBookingService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<AdminBookingService> _logger;
        private readonly IAdminNotifier? _adminNotifier;
        private readonly ITierService? _tierService;
        private readonly IPointService? _pointService;

        // adminNotifier/tierService/pointService có default null để các unit test cũ (chỉ truyền context + logger) không phải sửa.
        public AdminBookingService(IApplicationDbContext context, ILogger<AdminBookingService> logger, IAdminNotifier? adminNotifier = null, ITierService? tierService = null, IPointService? pointService = null)
        {
            _context = context;
            _logger = logger;
            _adminNotifier = adminNotifier;
            _tierService = tierService;
            _pointService = pointService;
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

            return bookings.Select(b => new AdminBookingListResponse
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
                CreatedAt = b.CreatedAt
            });
        }

        public async Task<AdminBookingListResponse> GetBookingByIdAsync(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) throw new Exception("BOOKING_NOT_FOUND");

            var customer = await _context.Customers.FindAsync(booking.CustomerID);
            var service = await _context.Services.FindAsync(booking.ServiceID);

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
                CreatedAt = booking.CreatedAt
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

            var customer = await _context.Customers.FirstOrDefaultAsync(c =>
                (booking.CustomerID > 0 && c.CustomerID == booking.CustomerID) ||
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
