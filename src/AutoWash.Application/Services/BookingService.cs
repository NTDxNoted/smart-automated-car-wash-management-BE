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
    public class BookingService : IBookingService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<BookingService> _logger;

        public BookingService(IApplicationDbContext context, ILogger<BookingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // --- TASK 1: DANH SÁCH BOOKING CÓ PHÂN TRANG ---
        public async Task<PagedResponse<BookingResponseDto>> GetCustomerBookingsAsync(int customerId, string? status, int page, int pageSize)
        {
            var query = _context.Bookings.Where(b => b.CustomerID == customerId).AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<BookingStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(b => b.Status == parsedStatus);
            }

            var total = await query.CountAsync();
            var bookings = await query
                .OrderByDescending(b => b.ScheduledTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BookingResponseDto
                {
                    BookingId = b.BookingID,
                    LicensePlate = b.LicensePlate,
                    // Giả định ServiceName sẽ được map qua navigation property
                    ServiceName = "Auto-mapped from Service entity",
                    ScheduledTime = b.ScheduledTime,
                    Status = b.Status.ToString(),
                    FinalAmount = b.FinalAmount,
                    PointsEarned = b.PointsEarned
                })
                .ToListAsync();

            return new PagedResponse<BookingResponseDto> { Page = page, Total = total, Data = bookings };
        }

        // --- TASK 2: CHI TIẾT 1 BOOKING ---
        public async Task<BookingResponseDto> GetBookingByIdAsync(int bookingId, int customerId)
        {
            var booking = await _context.Bookings
                .Where(b => b.BookingID == bookingId && b.CustomerID == customerId)
                .FirstOrDefaultAsync();

            if (booking == null) throw new Exception("Không tìm thấy lịch đặt.");

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

        // --- TASK 3: LOGIC HỦY LỊCH (CORE ISSUE 07) ---
        public async Task<CancelBookingResponseDto> CancelBookingAsync(int bookingId, int customerId)
        {
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingID == bookingId && b.CustomerID == customerId);

            if (booking == null)
                throw new Exception("NOT_FOUND: Không tìm thấy lịch đặt.");

            // BR-64: Chỉ cho phép thao tác khi đơn đang Pending
            if (booking.Status != BookingStatus.Pending)
                throw new Exception("INVALID_STATUS: Đơn này không thể hủy vì không ở trạng thái Pending.");

            // BR-63: Phải hủy trước giờ hẹn >= 2 tiếng
            var hoursUntilSchedule = (booking.ScheduledTime - DateTime.UtcNow).TotalHours;
            if (hoursUntilSchedule < 2)
                throw new Exception("CANCEL_TOO_LATE: Chỉ được hủy trước giờ hẹn ít nhất 2 tiếng.");

            // Bắt đầu quá trình hủy
            booking.Status = BookingStatus.Cancelled;
            int pointsRefunded = 0;

            // BR-62: Logic hoàn điểm giữ nguyên ExpiredAt
            if (booking.PointsRedeemed > 0)
            {
                // Tìm lại giao dịch trừ điểm gốc
                var originalTxn = await _context.PointTransactions
                    .Where(pt => pt.RefBookingID == booking.BookingID && pt.Type == PointTransactionType.Redeem)
                    .FirstOrDefaultAsync();

                var loyaltyAccount = await _context.LoyaltyAccounts
                    .FirstOrDefaultAsync(la => la.CustomerID == customerId);

                if (loyaltyAccount != null && originalTxn != null)
                {
                    // Trả lại điểm vào ví
                    loyaltyAccount.TotalPoints += booking.PointsRedeemed;
                    loyaltyAccount.LastUpdated = DateTime.UtcNow;

                    // Tạo lịch sử cộng điểm nhưng sao chép hạn sử dụng cũ
                    var refundTxn = new PointTransaction
                    {
                        LoyaltyID = loyaltyAccount.LoyaltyID,
                        Points = booking.PointsRedeemed,
                        Type = PointTransactionType.Earn, // Hoàn lại như một khoản cộng vào
                        RefBookingID = booking.BookingID,
                        ExpiredAt = originalTxn.ExpiredAt, // Giữ nguyên hạn sử dụng gốc
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.PointTransactions.Add(refundTxn);
                    pointsRefunded = booking.PointsRedeemed;
                }
            }

            await _context.SaveChangesAsync();

            // Trigger Notification Log
            _logger.LogInformation($"[CANCEL_LOG] Khách hàng {customerId} đã hủy Booking {bookingId}. Số điểm hoàn lại: {pointsRefunded}");

            return new CancelBookingResponseDto
            {
                BookingId = booking.BookingID,
                Status = booking.Status.ToString(),
                PointsRefunded = pointsRefunded,
                Message = $"Hủy lịch thành công. {pointsRefunded} điểm đã được hoàn trả."
            };
        }
    }
}