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

        public async Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request, int? customerId = null)
        {
            if (request == null) throw new Exception("INVALID_REQUEST: Thiếu dữ liệu đặt lịch.");
            if (request.ServiceId <= 0) throw new Exception("INVALID_REQUEST: Thiếu dịch vụ.");

            var service = await _context.Services.FirstOrDefaultAsync(s => s.ServiceID == request.ServiceId && s.Status == "Active");
            if (service == null) throw new Exception("NOT_FOUND: Không tìm thấy dịch vụ.");

            if (request.ScheduledTime < DateTime.UtcNow.AddHours(1))
                throw new Exception("ADVANCE_NOTICE_TOO_SHORT: Booking phải cách giờ hẹn tối thiểu 60 phút.");

            Customer? customer = null;
            Vehicle? vehicle = null;

            if (customerId.HasValue)
            {
                customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerID == customerId.Value);
                if (customer == null) throw new Exception("NOT_FOUND: Không tìm thấy khách hàng.");
                if (customer.SuspendedUntil.HasValue && customer.SuspendedUntil.Value > DateTime.UtcNow)
                    throw new Exception("ACCOUNT_SUSPENDED: Tài khoản đang bị khóa.");

                if (request.VehicleId.HasValue)
                {
                    vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.VehicleID == request.VehicleId.Value && v.CustomerID == customerId.Value && v.IsActive);
                }

                if (vehicle == null) throw new Exception("NOT_FOUND: Không tìm thấy xe của khách hàng.");

                var pendingCount = await _context.Bookings.CountAsync(b => b.CustomerID == customerId.Value && b.Status == BookingStatus.Pending);
                if (pendingCount >= 3) throw new Exception("PENDING_QUOTA_EXCEEDED: Bạn đã có 3 lịch hẹn đang chờ xác nhận.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.Phone) || string.IsNullOrWhiteSpace(request.LicensePlate))
                    throw new Exception("INVALID_REQUEST: Cần số điện thoại và biển số xe.");

                vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.LicensePlate == request.LicensePlate.Trim() && v.IsActive);
                if (vehicle == null) throw new Exception("NOT_FOUND: Không tìm thấy xe tương ứng với biển số.");

                var pendingCount = await _context.Bookings.CountAsync(b => b.Phone == request.Phone.Trim() && b.Status == BookingStatus.Pending);
                if (pendingCount >= 1) throw new Exception("PENDING_QUOTA_EXCEEDED: Bạn đã có 1 lịch hẹn đang chờ xác nhận.");
            }

            var samePlateConflict = await _context.Bookings.AnyAsync(b =>
                b.LicensePlate == vehicle.LicensePlate &&
                b.Status != BookingStatus.Cancelled &&
                b.Status != BookingStatus.Completed &&
                b.Status != BookingStatus.Failed &&
                b.ScheduledTime >= request.ScheduledTime.AddMinutes(-120) &&
                b.ScheduledTime <= request.ScheduledTime.AddMinutes(120));
            if (samePlateConflict) throw new Exception("VEHICLE_BUFFER_VIOLATION: Biển số này đã có lịch hẹn trong vòng 120 phút.");

            var baseAmount = service.Price;
            decimal tierDiscount = 0m;
            decimal rewardDiscount = 0m;
            decimal promotionDiscount = 0m;

            if (customer != null)
            {
                var tier = await _context.Tiers.FirstOrDefaultAsync(t => t.TierID == customer.TierID);
                tierDiscount = Math.Round(baseAmount * ((tier?.DiscountRate ?? 0m) / 100m), 0);
            }

            var finalAmount = baseAmount - tierDiscount - rewardDiscount - promotionDiscount;
            if (finalAmount < 0) finalAmount = 0;

            var booking = new Booking
            {
                CustomerID = customerId ?? 0,
                Phone = customer?.Phone ?? request.Phone ?? string.Empty,
                VehicleID = vehicle.VehicleID,
                LicensePlate = vehicle.LicensePlate,
                ServiceID = service.ServiceID,
                RewardID = request.RewardId,
                PromotionID = null,
                ScheduledTime = request.ScheduledTime,
                Status = BookingStatus.Pending,
                BaseAmount = baseAmount,
                DiscountApplied = tierDiscount + rewardDiscount + promotionDiscount,
                FinalAmount = finalAmount,
                PointsEarned = 0,
                PointsRedeemed = 0,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = null
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return new BookingResponse
            {
                BookingId = booking.BookingID,
                Phone = booking.Phone,
                LicensePlate = booking.LicensePlate,
                Service = new { service.ServiceID, service.ServiceName, service.Duration },
                ScheduledTime = booking.ScheduledTime,
                Status = booking.Status.ToString(),
                Invoice = new InvoiceSummary
                {
                    BaseAmount = booking.BaseAmount,
                    TierDiscount = tierDiscount,
                    RewardDiscount = rewardDiscount,
                    PromotionDiscount = promotionDiscount,
                    DiscountApplied = booking.DiscountApplied,
                    FinalAmount = booking.FinalAmount
                },
                PointsWillEarn = 0,
                CreatedAt = booking.CreatedAt
            };
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