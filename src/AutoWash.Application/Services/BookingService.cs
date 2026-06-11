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

        public BookingService(
            IApplicationDbContext context,
            ILogger<BookingService> logger,
            ITierService tierService)
        {
            _context = context;
            _logger = logger;
            _tierService = tierService;
        }

        // POST /api/Bookings
        public async Task<BookingResponseDto> CreateBookingAsync(CreateBookingRequest request, int? customerId)
        {
            if (request.ServiceId <= 0)
                throw new Exception("SERVICE_REQUIRED: Vui lòng chọn dịch vụ.");

            if (request.ScheduledTime <= DateTime.UtcNow.AddMinutes(60))
                throw new Exception("ADVANCE_NOTICE_VIOLATION: Phải đặt lịch trước ít nhất 60 phút.");

            string phone;
            string licensePlate;

            if (customerId.HasValue)
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.CustomerID == customerId.Value);

                if (customer == null)
                    throw new Exception("CUSTOMER_NOT_FOUND: Không tìm thấy khách hàng.");

                if (customer.SuspendedUntil.HasValue &&
                    customer.SuspendedUntil.Value > DateTime.UtcNow)
                    throw new Exception("SUSPENDED_ACCOUNT: Tài khoản đang bị tạm khóa.");

                phone = customer.Phone;

                if (!request.VehicleId.HasValue)
                    throw new Exception("VEHICLE_REQUIRED: Member phải truyền vehicleId.");

                var vehicle = await _context.Vehicles
                    .FirstOrDefaultAsync(v =>
                        v.VehicleID == request.VehicleId.Value &&
                        v.CustomerID == customerId.Value);

                if (vehicle == null)
                    throw new Exception("VEHICLE_NOT_FOUND: Không tìm thấy xe của khách hàng.");

                licensePlate = vehicle.LicensePlate;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.Phone))
                    throw new Exception("PHONE_REQUIRED: Guest phải nhập số điện thoại.");

                if (string.IsNullOrWhiteSpace(request.LicensePlate))
                    throw new Exception("LICENSE_PLATE_REQUIRED: Guest phải nhập biển số xe.");

                phone = request.Phone.Trim();
                licensePlate = request.LicensePlate.Trim();
            }

            var pendingCount = await _context.Bookings.CountAsync(b =>
                b.Status == BookingStatus.Pending &&
                (
                    customerId.HasValue
                        ? b.CustomerID == customerId.Value
                        : b.Phone == phone
                ));

            if (!customerId.HasValue && pendingCount >= 1)
                throw new Exception("PENDING_QUOTA_EXCEEDED: Guest chỉ được có tối đa 1 lịch hẹn đang chờ.");

            if (customerId.HasValue && pendingCount >= 3)
                throw new Exception("PENDING_QUOTA_EXCEEDED: Bạn đã có 3 lịch hẹn đang chờ xác nhận.");

            var sameDayCount = await _context.Bookings.CountAsync(b =>
                b.ScheduledTime.Date == request.ScheduledTime.Date &&
                b.Status != BookingStatus.Cancelled &&
                (
                    customerId.HasValue
                        ? b.CustomerID == customerId.Value
                        : b.Phone == phone
                ));

            if (sameDayCount >= 2)
                throw new Exception("DAILY_BOOKING_LIMIT: Không được có quá 2 booking chưa hoàn thành trong ngày.");

            var bufferStart = request.ScheduledTime.AddMinutes(-120);
            var bufferEnd = request.ScheduledTime.AddMinutes(120);

            bool vehicleBufferViolated = await _context.Bookings.AnyAsync(b =>
                b.LicensePlate == licensePlate &&
                b.Status != BookingStatus.Cancelled &&
                b.ScheduledTime >= bufferStart &&
                b.ScheduledTime <= bufferEnd);

            if (vehicleBufferViolated)
                throw new Exception("VEHICLE_BUFFER_VIOLATION: Biển số này đã có lịch hẹn trong vòng 120 phút.");

            bool slotTaken = await _context.Bookings.AnyAsync(b =>
                b.Status != BookingStatus.Cancelled &&
                b.ScheduledTime >= request.ScheduledTime.AddMinutes(-5) &&
                b.ScheduledTime <= request.ScheduledTime.AddMinutes(5));

            if (slotTaken)
                throw new Exception("SLOT_NOT_AVAILABLE: Khung giờ này đã có người đặt.");

            var service = await _context.Services
                .FirstOrDefaultAsync(s => s.ServiceID == request.ServiceId);

            if (service == null)
                throw new Exception("SERVICE_NOT_FOUND: Không tìm thấy dịch vụ.");

            decimal baseAmount = service.Price;
            decimal finalAmount = baseAmount;

            var booking = new Booking
            {
                CustomerID = customerId ?? 0,
                Phone = phone,
                LicensePlate = licensePlate,
                ServiceID = request.ServiceId,
                ScheduledTime = request.ScheduledTime,
                Status = BookingStatus.Pending,

                BaseAmount = baseAmount,
                FinalAmount = finalAmount,

                PointsEarned = (int)(finalAmount / 10000),
                CreatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return new BookingResponseDto
            {
                BookingId = booking.BookingID,
                LicensePlate = booking.LicensePlate,
                ServiceName = service.ServiceName,
                ScheduledTime = booking.ScheduledTime,
                Status = booking.Status.ToString(),
                FinalAmount = booking.FinalAmount,
                PointsEarned = booking.PointsEarned
            };
        }

        // GET /api/Bookings
        public async Task<PagedResponse<BookingResponseDto>> GetCustomerBookingsAsync(
            int? customerId,
            string? guestPhone,
            string? status,
            int page,
            int pageSize)
        {
            var query = _context.Bookings.AsQueryable();

            if (customerId.HasValue)
                query = query.Where(b => b.CustomerID == customerId.Value);
            else if (!string.IsNullOrEmpty(guestPhone))
                query = query.Where(b => b.Phone == guestPhone);
            else
                throw new Exception("UNAUTHORIZED: Cần cung cấp ID hoặc SĐT.");

            if (!string.IsNullOrEmpty(status) &&
                Enum.TryParse<BookingStatus>(status, true, out var parsedStatus))
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
                    ScheduledTime = b.ScheduledTime,
                    Status = b.Status.ToString(),
                    FinalAmount = b.FinalAmount,
                    PointsEarned = b.PointsEarned
                })
                .ToListAsync();

            return new PagedResponse<BookingResponseDto>
            {
                Page = page,
                Total = total,
                Data = bookings
            };
        }

        // GET /api/Bookings/{id}
        public async Task<BookingResponseDto> GetBookingByIdAsync(
            int bookingId,
            int? customerId,
            string? guestPhone)
        {
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingID == bookingId);

            if (booking == null)
                throw new Exception("NOT_FOUND: Không tìm thấy lịch đặt.");

            if (customerId.HasValue && booking.CustomerID != customerId.Value)
                throw new Exception("UNAUTHORIZED: Không có quyền truy cập.");

            if (!customerId.HasValue && booking.Phone != guestPhone)
                throw new Exception("UNAUTHORIZED: Không có quyền truy cập.");

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

        // POST /api/Bookings/{id}/cancel
        public async Task<CancelBookingResponseDto> CancelBookingAsync(
            int bookingId,
            int? customerId,
            string? guestPhone)
        {
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingID == bookingId);

            if (booking == null)
                throw new Exception("NOT_FOUND: Không tìm thấy lịch đặt.");

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

        // POST /api/Bookings/{id}/complete
        public async Task<BookingResponseDto> CompleteBookingAsync(int bookingId)
        {
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingID == bookingId);

            if (booking == null)
                throw new Exception("NOT_FOUND: Không tìm thấy lịch đặt.");

            if (booking.Status != BookingStatus.Pending)
                throw new Exception("INVALID_STATUS: Chỉ có thể hoàn thành booking ở trạng thái Pending.");

            booking.Status = BookingStatus.Completed;
            booking.CompletedAt = DateTime.UtcNow;

            if (booking.CustomerID > 0)
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.CustomerID == booking.CustomerID);

                if (customer != null)
                {
                    customer.TotalSpending += booking.FinalAmount;

                    await _context.SaveChangesAsync();

                    await _tierService.EvaluateUpgradeAsync(customer.CustomerID);

                    _logger.LogInformation(
                        "[CompleteBooking] BookingID={Id} completed, CustomerID={Cid}, Amount={Amt}",
                        bookingId,
                        customer.CustomerID,
                        booking.FinalAmount);
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