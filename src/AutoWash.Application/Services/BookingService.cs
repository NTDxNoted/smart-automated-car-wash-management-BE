using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Entities;
using AutoWash.Domain.Enums;

namespace AutoWash.Application.Services
{
    public class BookingService : IBookingsService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<BookingService> _logger;
        private readonly IBookingValidationService _bookingValidationService;
        private readonly IInvoiceService _invoiceService;

        public BookingService(
            IApplicationDbContext context,
            ILogger<BookingService> logger,
            IBookingValidationService bookingValidationService,
            IInvoiceService invoiceService)
        {
            _context = context;
            _logger = logger;
            _bookingValidationService = bookingValidationService;
            _invoiceService = invoiceService;
        }

        public async Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request, int? customerId)
        {
            Customer? customer = null;
            string licensePlate;

            if (customerId.HasValue)
            {
                customer = await _context.Customers.FindAsync(customerId.Value);
                if (customer == null)
                    throw new InvalidOperationException("UNAUTHORIZED: Không tìm thấy thành viên.");

                if (request.VehicleId == null)
                    throw new InvalidOperationException("INVALID_REQUEST: Member cần cung cấp vehicleId.");

                var vehicle = await _context.Vehicles
                    .FirstOrDefaultAsync(v => v.VehicleID == request.VehicleId && v.CustomerID == customerId.Value);

                if (vehicle == null)
                    throw new InvalidOperationException("INVALID_REQUEST: Xe không tồn tại hoặc không thuộc thành viên.");

                licensePlate = vehicle.LicensePlate;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.Phone) || string.IsNullOrWhiteSpace(request.LicensePlate))
                    throw new InvalidOperationException("INVALID_REQUEST: Guest phải cung cấp phone và licensePlate.");

                licensePlate = request.LicensePlate.Trim();
            }

            var service = await _context.Services.FindAsync(request.ServiceId);
            if (service == null || !service.IsActive)
                throw new InvalidOperationException("INVALID_SERVICE: Dịch vụ không hợp lệ.");

            await _bookingValidationService.ValidateCreateBookingAsync(request, customer, service, licensePlate);

            Promotion? promotion = null;
            decimal? promotionDiscountAmount = null;
            if (!string.IsNullOrWhiteSpace(request.PromoCode))
            {
                promotion = await _context.Promotions
                    .FirstOrDefaultAsync(p => p.Code == request.PromoCode.Trim() && p.IsActive);

                if (promotion == null)
                    throw new InvalidOperationException("INVALID_PROMO_CODE: Mã khuyến mãi không hợp lệ.");

                promotionDiscountAmount = promotion.DiscountAmount;
            }

            Reward? reward = null;
            decimal? rewardDiscountAmount = null;
            LoyaltyAccount? loyaltyAccount = null;
            if (request.RewardId.HasValue)
            {
                if (customer == null)
                    throw new InvalidOperationException("INVALID_REWARD: Chỉ thành viên mới được sử dụng reward.");

                reward = await _context.Rewards.FindAsync(request.RewardId.Value);
                if (reward == null || !reward.IsActive)
                    throw new InvalidOperationException("INVALID_REWARD: Mã reward không hợp lệ.");

                loyaltyAccount = await _context.LoyaltyAccounts
                    .FirstOrDefaultAsync(l => l.CustomerID == customer.CustomerID);

                if (loyaltyAccount == null || loyaltyAccount.TotalPoints < reward.PointsRequired)
                    throw new InvalidOperationException("INVALID_REWARD: Không đủ điểm để sử dụng.");

                if (reward.DiscountAmount > service.BaseAmount * 0.5m)
                    throw new InvalidOperationException("INVALID_REWARD: Giảm giá vượt quá 50% hóa đơn.");

                rewardDiscountAmount = reward.DiscountAmount;
            }

            var invoice = _invoiceService.CalculateInvoice(
                service.BaseAmount,
                customer?.TierID ?? 1,
                request.RewardId,
                rewardDiscountAmount,
                request.PromoCode,
                promotionDiscountAmount);

            var pointsWillEarn = (int)Math.Floor(invoice.FinalAmount / 10000m);

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var booking = new Booking
            {
                CustomerID = customer?.CustomerID ?? 0,
                Phone = customer?.Phone ?? request.Phone!.Trim(),
                VehicleID = request.VehicleId ?? 0,
                LicensePlate = licensePlate,
                ServiceID = service.ServiceID,
                RewardID = request.RewardId,
                PromotionID = promotion?.PromotionID,
                ScheduledTime = request.ScheduledTime,
                BaseAmount = service.BaseAmount,
                DiscountApplied = invoice.DiscountApplied,
                FinalAmount = invoice.FinalAmount,
                PointsEarned = pointsWillEarn,
                PointsRedeemed = reward?.PointsRequired ?? 0,
                CreatedAt = DateTime.UtcNow,
                Status = BookingStatus.Pending
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            if (promotion != null && customer != null)
            {
                _context.CustomerPromotions.Add(new CustomerPromotion
                {
                    CustomerID = customer.CustomerID,
                    PromotionID = promotion.PromotionID,
                    BookingID = booking.BookingID,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (reward != null && loyaltyAccount != null)
            {
                _context.PointTransactions.Add(new PointTransaction
                {
                    LoyaltyID = loyaltyAccount.LoyaltyID,
                    Points = -reward.PointsRequired,
                    Type = PointTransactionType.Redeem,
                    RefBookingID = booking.BookingID,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Booking created successfully for {LicensePlate} at {ScheduleTime}", licensePlate, request.ScheduledTime);

            return new BookingResponse
            {
                BookingId = booking.BookingID,
                Phone = booking.Phone,
                LicensePlate = booking.LicensePlate,
                Service = new ServiceInfoDto
                {
                    ServiceId = service.ServiceID,
                    ServiceName = service.ServiceName,
                    Duration = service.Duration
                },
                ScheduledTime = booking.ScheduledTime,
                Status = booking.Status.ToString(),
                Invoice = invoice,
                PointsWillEarn = pointsWillEarn,
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
    }
}