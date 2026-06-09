using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Enums;
using AutoWash.Domain.Entities;

namespace AutoWash.Application.Services
{
    public class BookingService : IBookingsService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<BookingService> _logger;
        private readonly ITierService _tierService;

        public BookingService(IApplicationDbContext context, ILogger<BookingService> logger, ITierService tierService)
        {
            _context = context;
            _logger = logger;
            _tierService = tierService;
        }

        // 1. GET DANH SÁCH (Phân trang & Filter)
        public async Task<PagedResponse<BookingResponseDto>> GetCustomerBookingsAsync(int? customerId, string? guestPhone, string? status, int page, int pageSize)
        {
            var query = _context.Bookings.AsQueryable();

            if (customerId.HasValue)
                query = query.Where(b => b.CustomerID == customerId.Value);
            else if (!string.IsNullOrEmpty(guestPhone))
                query = query.Where(b => b.Phone == guestPhone);
            else
                throw new Exception("UNAUTHORIZED: Cần cung cấp ID hoặc SĐT.");

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<BookingStatus>(status, true, out var parsedStatus))
                query = query.Where(b => b.Status == parsedStatus);

            var total = await query.CountAsync();
            var bookings = await query
                .OrderByDescending(b => b.ScheduledTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BookingResponseDto
                {
                    BookingId = b.BookingID,
                    LicensePlate = b.LicensePlate,
                    ServiceName = "Auto-mapped from Service entity",
                    ScheduledTime = b.ScheduledTime,
                    Status = b.Status.ToString(),
                    FinalAmount = b.FinalAmount,
                    PointsEarned = b.PointsEarned
                }).ToListAsync();

            return new PagedResponse<BookingResponseDto> { Page = page, Total = total, Data = bookings };
        }

        // 2. GET CHI TIẾT
        public async Task<BookingResponseDto> GetBookingByIdAsync(int bookingId, int? customerId, string? guestPhone)
        {
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingID == bookingId);

            if (booking == null) throw new Exception("NOT_FOUND: Không tìm thấy lịch đặt.");

            if (customerId.HasValue && booking.CustomerID != customerId) throw new Exception("UNAUTHORIZED: Không có quyền truy cập.");
            if (!customerId.HasValue && booking.Phone != guestPhone) throw new Exception("UNAUTHORIZED: Không có quyền truy cập.");

            return new BookingResponseDto
            {
                BookingId = booking.BookingID,
                LicensePlate = booking.LicensePlate,
                ScheduledTime = booking.ScheduledTime,
                Status = booking.Status.ToString(),
                FinalAmount = booking.FinalAmount,
                PointsEarned = booking.PointsEarned
            };
        }

        // 3. CANCEL BOOKING
        public async Task<CancelBookingResponseDto> CancelBookingAsync(int bookingId, int? customerId, string? guestPhone)
        {
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingID == bookingId);
            if (booking == null) throw new Exception("NOT_FOUND: Không tìm thấy lịch đặt.");

            // Xác thực quyền
            if (customerId.HasValue)
            {
                if (booking.CustomerID != customerId.Value)
                    throw new Exception("UNAUTHORIZED: Bạn không có quyền hủy lịch này.");
            }
            else if (!string.IsNullOrEmpty(guestPhone))
            {
                if (booking.Phone != guestPhone)
                    throw new Exception("UNAUTHORIZED: Số điện thoại không khớp với lịch đặt.");
            }
            else
            {
                throw new Exception("UNAUTHORIZED: Cần thông tin đăng nhập hoặc SĐT.");
            }

            // Check Status & Time
            if (booking.Status != BookingStatus.Pending)
                throw new Exception("INVALID_STATUS: Đơn này không thể hủy.");

            if ((booking.ScheduledTime - DateTime.UtcNow).TotalHours < 2)
                throw new Exception("CANCEL_TOO_LATE: Chỉ được hủy trước giờ hẹn 2 tiếng.");

            booking.Status = BookingStatus.Cancelled;
            await _context.SaveChangesAsync();

            return new CancelBookingResponseDto
            {
                BookingId = booking.BookingID,
                Status = "Cancelled",
                Message = "Hủy lịch thành công."
            };
        }

        public Task GetCustomerBookingsAsync(int customerId, string? status, int page, int pageSize)
        {
            throw new NotImplementedException();
        }

        // BR-21: Staff/Admin hoàn tất booking → cập nhật TotalSpending → trigger upgrade tier
        public async Task<BookingResponseDto> CompleteBookingAsync(int bookingId)
        {
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingID == bookingId);
            if (booking == null) throw new Exception("NOT_FOUND: Không tìm thấy lịch đặt.");

            if (booking.Status != BookingStatus.Pending)
                throw new Exception("INVALID_STATUS: Chỉ có thể hoàn thành booking ở trạng thái Pending.");

            booking.Status = BookingStatus.Completed;
            booking.CompletedAt = DateTime.UtcNow;

            // Cộng TotalSpending cho member (CustomerID = 0 nghĩa là guest)
            if (booking.CustomerID > 0)
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.CustomerID == booking.CustomerID);

                if (customer != null)
                {
                    customer.TotalSpending += booking.FinalAmount;
                    await _context.SaveChangesAsync();

                    // BR-21: evaluate upgrade real-time
                    await _tierService.EvaluateUpgradeAsync(customer.CustomerID);

                    _logger.LogInformation("[CompleteBooking] BookingID={Id} completed, CustomerID={Cid}, Amount={Amt}",
                        bookingId, customer.CustomerID, booking.FinalAmount);
                    return new BookingResponseDto
                    {
                        BookingId = booking.BookingID,
                        LicensePlate = booking.LicensePlate,
                        ScheduledTime = booking.ScheduledTime,
                        Status = booking.Status.ToString(),
                        FinalAmount = booking.FinalAmount,
                        PointsEarned = booking.PointsEarned
                    };
                }
            }

            await _context.SaveChangesAsync();

            return new BookingResponseDto
            {
                BookingId = booking.BookingID,
                LicensePlate = booking.LicensePlate,
                ScheduledTime = booking.ScheduledTime,
                Status = booking.Status.ToString(),
                FinalAmount = booking.FinalAmount,
                PointsEarned = booking.PointsEarned
            };
        }
    }
}