using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoWash.Application.DTOs.Admin;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutoWash.Application.Services
{
    public class AdminBookingService : IAdminBookingService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<AdminBookingService> _logger;

        public AdminBookingService(IApplicationDbContext context, ILogger<AdminBookingService> logger)
        {
            _context = context;
            _logger = logger;
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

            if (newStatus == BookingStatus.Completed || newStatus == BookingStatus.Failed || newStatus == BookingStatus.Cancelled)
            {
                booking.CompletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Booking {BookingID} status updated from {OldStatus} to {NewStatus}", id, previousStatus, newStatus);

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

        public async Task<object> EmergencyStopAsync(int id, EmergencyStopRequest request)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) throw new Exception("BOOKING_NOT_FOUND");

            _logger.LogError("EMERGENCY STOP TRIGGERED for Booking {BookingID}. Reason: {Reason}", id, request.Reason);
            
            if (booking.Status == BookingStatus.Pending)
            {
                booking.Status = BookingStatus.Failed;
                booking.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
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
