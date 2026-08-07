using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AutoWash.Application.Common.Validation;
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
        private readonly IPointService? _pointService;
        private readonly BookingSettings _bookingSettings;
        private readonly IAdminNotifier? _adminNotifier;
        private readonly IBookingHubNotifier? _bookingHubNotifier;

        public BookingService(
            IApplicationDbContext context,
            ILogger<BookingService> logger,
            ITierService tierService)
            : this(context, logger, tierService, null, Microsoft.Extensions.Options.Options.Create(new BookingSettings()), null, null)
        {
        }

        public BookingService(
            IApplicationDbContext context,
            ILogger<BookingService> logger,
            ITierService tierService,
            IPointService? pointService)
            : this(context, logger, tierService, pointService, Microsoft.Extensions.Options.Options.Create(new BookingSettings()), null, null)
        {
        }

        public BookingService(
            IApplicationDbContext context,
            ILogger<BookingService> logger,
            ITierService tierService,
            IOptions<BookingSettings> bookingSettings)
            : this(context, logger, tierService, null, bookingSettings, null, null)
        {
        }

        // Constructor thật dùng lúc runtime (DI resolve) — IAdminNotifier/IBookingHubNotifier để null
        // (không truyền) vẫn chạy bình thường, chỉ là không đẩy real-time notification (dùng cho unit
        // test cũ không quan tâm tới SignalR, xem BookingServiceTests.cs).
        public BookingService(
            IApplicationDbContext context,
            ILogger<BookingService> logger,
            ITierService tierService,
            IPointService? pointService,
            IOptions<BookingSettings> bookingSettings,
            IAdminNotifier? adminNotifier,
            IBookingHubNotifier? bookingHubNotifier)
        {
            _context = context;
            _logger = logger;
            _tierService = tierService;
            _pointService = pointService;
            _bookingSettings = bookingSettings.Value;
            _adminNotifier = adminNotifier;
            _bookingHubNotifier = bookingHubNotifier;
        }

        // POST /api/Bookings
        public async Task<BookingResponseDto> CreateBookingAsync(CreateBookingRequest request, int? customerId)
        {
            if (request.ServiceId <= 0)
                throw new Exception("SERVICE_REQUIRED: Vui lòng chọn dịch vụ.");

            if (request.ScheduledTime <= DateTime.UtcNow.AddMinutes(60))
                throw new Exception("ADVANCE_NOTICE_VIOLATION: Phải đặt lịch trước ít nhất 60 phút.");

            Customer? customer = null;
            Tier? tier = null;
            LoyaltyAccount? loyalty = null;
            Vehicle? selectedVehicle = null;
            int? effectiveCustomerId = customerId;

            string phone;
            string licensePlate;

            if (customerId.HasValue)
            {
                customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.CustomerID == customerId.Value);

                if (customer == null)
                    throw new Exception("CUSTOMER_NOT_FOUND: Không tìm thấy khách hàng.");

                if (customer.IsLocked)
                    throw new Exception("ACCOUNT_LOCKED: Tài khoản đang bị khóa.");

                if (customer.SuspendedUntil.HasValue &&
                    customer.SuspendedUntil.Value > DateTime.UtcNow)
                    throw new Exception("SUSPENDED_ACCOUNT: Tài khoản đang bị tạm khóa.");

                tier = await _context.Tiers.FirstOrDefaultAsync(t => t.TierID == customer.TierID);
                if (tier == null)
                    throw new Exception("TIER_NOT_FOUND: Không tìm thấy tier của khách hàng.");

                loyalty = await _context.LoyaltyAccounts.FirstOrDefaultAsync(l => l.CustomerID == customer.CustomerID);

                phone = customer.Phone;
 
                if (request.VehicleId.HasValue && request.VehicleId.Value > 0)
                {
                    selectedVehicle = await _context.Vehicles
                        .FirstOrDefaultAsync(v =>
                            v.VehicleID == request.VehicleId.Value &&
                            v.CustomerID == customerId.Value);
 
                    if (selectedVehicle == null)
                        throw new Exception("VEHICLE_NOT_FOUND: Không tìm thấy xe của khách hàng.");
 
                    licensePlate = selectedVehicle.LicensePlate;
                }
                else if (!string.IsNullOrWhiteSpace(request.LicensePlate))
                {
                    if (!LicensePlateValidator.IsValid(request.LicensePlate))
                        throw new Exception("INVALID_LICENSE_PLATE: Biển số xe không đúng định dạng chuẩn Việt Nam (VD: 30F-123.45, 51F12345).");

                    var cleanPlate = request.LicensePlate.Trim().ToUpper();
                    selectedVehicle = await _context.Vehicles
                        .FirstOrDefaultAsync(v =>
                            v.CustomerID == customerId.Value &&
                            v.LicensePlate == cleanPlate);
 
                    if (selectedVehicle == null)
                    {
                        selectedVehicle = new Vehicle
                        {
                            CustomerID = customerId.Value,
                            LicensePlate = cleanPlate,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Vehicles.Add(selectedVehicle);
                        await _context.SaveChangesAsync();
                    }
 
                    licensePlate = selectedVehicle.LicensePlate;
                }
                else
                {
                    throw new Exception("VEHICLE_REQUIRED: Vui lòng chọn xe hoặc nhập biển số xe.");
                }
 
                if (request.ScheduledTime > DateTime.UtcNow.AddDays(tier.BookingWindowDays))
                    throw new Exception("BOOKING_WINDOW_VIOLATION: Lịch đặt vượt quá khung thời gian theo tier.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.Phone))
                    throw new Exception("PHONE_REQUIRED: Guest phải nhập số điện thoại.");

                if (string.IsNullOrWhiteSpace(request.LicensePlate))
                    throw new Exception("LICENSE_PLATE_REQUIRED: Guest phải nhập biển số xe.");

                if (!LicensePlateValidator.IsValid(request.LicensePlate))
                    throw new Exception("INVALID_LICENSE_PLATE: Biển số xe không đúng định dạng chuẩn Việt Nam (VD: 30F-123.45, 51F12345).");

                phone = request.Phone.Trim();
                licensePlate = request.LicensePlate.Trim();

                customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Phone == "GUEST" && c.FullName == "Khách vãng lai");

                if (customer == null)
                {
                    customer = new Customer
                    {
                        FullName = "Khách vãng lai",
                        Phone = "GUEST",
                        Password = "GUEST",
                        Role = "MEMBER",
                        TierID = 1,
                        IsLocked = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                }

                effectiveCustomerId = customer.CustomerID;
                selectedVehicle = await _context.Vehicles
    .FirstOrDefaultAsync(v =>
        v.CustomerID == customer.CustomerID &&
        v.LicensePlate == licensePlate);

                if (selectedVehicle == null)
                {
                    selectedVehicle = new Vehicle
                    {
                        CustomerID = customer.CustomerID,
                        LicensePlate = licensePlate
                    };

                    _context.Vehicles.Add(selectedVehicle);
                    await _context.SaveChangesAsync();
                }

                tier = await _context.Tiers.FirstOrDefaultAsync(t => t.TierID == customer.TierID);
                if (tier == null)
                {
                    if (customer.Phone == "GUEST" && customer.FullName == "Khách vãng lai")
                    {
                        tier = new Tier
                        {
                            TierID = 1,
                            TierName = "Member",
                            MinSpending = 0,
                            BookingWindowDays = 7,
                            DiscountRate = 0,
                            PriorityScore = 1
                        };
                    }
                    else
                    {
                        throw new Exception("TIER_NOT_FOUND: Không tìm thấy tier của khách vãng lai.");
                    }
                }

                loyalty = await _context.LoyaltyAccounts.FirstOrDefaultAsync(l => l.CustomerID == customer.CustomerID);
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
                b.Status == BookingStatus.Pending &&
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
                b.Status != BookingStatus.Failed &&
                b.Status != BookingStatus.Completed &&
                b.ScheduledTime >= bufferStart &&
                b.ScheduledTime <= bufferEnd);

            if (vehicleBufferViolated)
                throw new Exception("VEHICLE_BUFFER_VIOLATION: Biển số xe này đã có đơn đặt lịch (Pending) trùng hoặc quá gần thời gian này.");

            var service = await _context.Services
                .FirstOrDefaultAsync(s => s.ServiceID == request.ServiceId);

            if (service == null)
                throw new Exception("SERVICE_NOT_FOUND: Không tìm thấy dịch vụ.");

            var newStartUtc = request.ScheduledTime.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(request.ScheduledTime, DateTimeKind.Utc)
                : request.ScheduledTime.ToUniversalTime();
            var newEndUtc = newStartUtc.AddMinutes(service.Duration + 5);

            // Fetch active bookings around requested time window in UTC
            var minCheckUtc = newStartUtc.AddHours(-12);
            var maxCheckUtc = newStartUtc.AddHours(12);

            // Advisory lock to prevent race conditions for same date
            IDbContextTransaction? bookingTransaction = null;
            if (_context is DbContext dbContextForLock)
            {
                bookingTransaction = await dbContextForLock.Database.BeginTransactionAsync();
                await _context.AcquireBookingDateLockAsync(newStartUtc.Date);
            }
            using var bookingTransactionScope = bookingTransaction;

            var activeBookings = await _context.Bookings
                .Where(b => b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.Failed && b.Status != BookingStatus.NoShow)
                .Where(b => b.ScheduledTime >= minCheckUtc && b.ScheduledTime <= maxCheckUtc)
                .Select(b => new
                {
                    b.ScheduledTime,
                    Duration = _context.Services.Where(s => s.ServiceID == b.ServiceID).Select(s => s.Duration).FirstOrDefault()
                })
                .ToListAsync();

            int overlapCount = 0;
            foreach (var b in activeBookings)
            {
                var bStartUtc = b.ScheduledTime.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(b.ScheduledTime, DateTimeKind.Utc)
                    : b.ScheduledTime.ToUniversalTime();
                var bEndUtc = bStartUtc.AddMinutes(b.Duration + 5);

                if (bStartUtc < newEndUtc && newStartUtc < bEndUtc)
                {
                    overlapCount++;
                }
            }

            int maxParallelSlots = _bookingSettings.MaxParallelSlots > 0 ? _bookingSettings.MaxParallelSlots : 1;
            if (overlapCount >= maxParallelSlots)
            {
                // BR-19: slot thường đã đầy — khách hạng cao hơn mức thấp nhất (Member/Guest)
                // vẫn được đặt thêm vào buffer ưu tiên, thay vì bị từ chối ngay như khách thường.
                var basePriorityScore = await _context.Tiers.MinAsync(t => (int?)t.PriorityScore) ?? 1;
                var priorityBuffer = _bookingSettings.PriorityBufferSlots > 0 ? _bookingSettings.PriorityBufferSlots : 0;
                bool isPriorityEligible = tier != null && tier.PriorityScore > basePriorityScore;

                if (!isPriorityEligible || overlapCount >= maxParallelSlots + priorityBuffer)
                    throw new Exception("SLOT_NOT_AVAILABLE: Khung giờ này đã đầy (đạt giới hạn số lượng xe rửa song song).");
            }

            decimal baseAmount = service.Price;
            decimal tierDiscount = 0m;
            decimal rewardDiscount = 0m;
            decimal promotionDiscount = 0m;

            if (tier != null)
            {
                tierDiscount = Math.Round(baseAmount * (tier.DiscountRate / 100m), 0);
            }

            RewardsCatalog? reward = null;
            if (request.RewardId.HasValue)
            {
                reward = await _context.RewardsCatalog.FirstOrDefaultAsync(r => r.RewardID == request.RewardId.Value && r.IsActive);
                if (reward == null)
                    throw new Exception("REWARD_NOT_FOUND: Không tìm thấy ưu đãi điểm.");

                if (loyalty == null || loyalty.TotalPoints < reward.PointsRequired)
                    throw new Exception("INSUFFICIENT_POINTS: Điểm không đủ để dùng ưu đãi này.");

                // BR-Rewards: % tính trên giá gốc dịch vụ; Fixed_Amount giữ nguyên số tiền.
                decimal rawRewardDiscount = reward.DiscountType == "Percentage"
                    ? Math.Round(baseAmount * (reward.DiscountAmount / 100m), 0)
                    : reward.DiscountAmount;

                // BR-60: điểm thưởng chỉ được thanh toán tối đa 50% giá trị hóa đơn.
                rewardDiscount = Math.Min(rawRewardDiscount, baseAmount * 0.50m);

                if (rewardDiscount < 0)
                    rewardDiscount = 0;
            }

            Promotion? promotion = null;
            if (!string.IsNullOrWhiteSpace(request.PromoCode))
            {
                promotion = await _context.Promotions.FirstOrDefaultAsync(p => p.PromoCode == request.PromoCode.Trim().ToUpperInvariant() && p.IsActive);
                if (promotion == null)
                    throw new Exception("PROMO_NOT_FOUND: Mã khuyến mãi không tồn tại.");

                var promoLocalToday = DateTime.UtcNow.AddHours(7).Date;
                if (promoLocalToday < promotion.StartDate.Date || promoLocalToday > promotion.EndDate.Date)
                    throw new Exception("PROMO_EXPIRED: Mã khuyến mãi đã hết hạn.");

                if (customer != null && promotion.MinTierID.HasValue && customer.TierID < promotion.MinTierID.Value)
                    throw new Exception("PROMO_TIER_NOT_ELIGIBLE: Bạn chưa đủ tier để dùng mã này.");

                var usedPromoCount = await _context.Bookings
                    .CountAsync(b => b.PromotionID == promotion.PromotionID && b.Status != BookingStatus.Cancelled);
                if (promotion.MaxUsage.HasValue && usedPromoCount >= promotion.MaxUsage.Value)
                    throw new Exception("PROMO_USAGE_LIMIT: Mã khuyến mãi đã hết lượt dùng.");

                if (customer != null)
                {
                    var customerUsedPromoCount = await _context.Bookings
                        .CountAsync(b => b.CustomerID == customer.CustomerID
                                       && b.PromotionID == promotion.PromotionID
                                       && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Completed));
                    if (customerUsedPromoCount >= 1)
                        throw new Exception("PROMO_USER_ALREADY_USED: Bạn đã sử dụng mã khuyến mãi này rồi.");
                }

                if (baseAmount < promotion.MinOrderValue)
                    throw new Exception($"PROMO_MIN_ORDER_NOT_MET: Đơn hàng cần tối thiểu {promotion.MinOrderValue:N0}đ để dùng mã này.");

                promotionDiscount = promotion.DiscountType == "Percentage"
                    ? Math.Round(baseAmount * (promotion.DiscountValue / 100m), 0)
                    : promotion.DiscountValue;

                if (promotion.MaxDiscountAmount.HasValue)
                    promotionDiscount = Math.Min(promotionDiscount, promotion.MaxDiscountAmount.Value);
            }

            decimal discountApplied = tierDiscount + rewardDiscount + promotionDiscount;
            decimal finalAmount = Math.Max(0, baseAmount - discountApplied);

            var booking = new Booking
            {
                CustomerID = effectiveCustomerId ?? 0,
                Phone = phone,
                LicensePlate = licensePlate,
                VehicleID = selectedVehicle!.VehicleID,
                ServiceID = request.ServiceId,
                RewardID = request.RewardId,
                PromotionID = promotion?.PromotionID,
                ScheduledTime = request.ScheduledTime,
                Status = BookingStatus.Pending,
                BaseAmount = baseAmount,
                DiscountApplied = discountApplied,
                FinalAmount = finalAmount,
                // Guest đặt lịch không đăng nhập dùng chung tài khoản "Khách vãng lai" (Password ==
                // "GUEST") không có loyalty tier thật — không tích điểm, khớp với PointService.EarnPointsAsync
                // và AdminBookingService.CreateWalkInBookingAsync.
                PointsEarned = customerId.HasValue
                    ? (int)Math.Max(0, Math.Floor(finalAmount / 10000m))
                    : 0,
                PointsRedeemed = reward?.PointsRequired ?? 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            if (bookingTransaction != null)
            {
                await bookingTransaction.CommitAsync();
            }

            if (reward != null && loyalty != null)
            {
                loyalty.TotalPoints -= reward.PointsRequired;
                loyalty.LastUpdated = DateTime.UtcNow;

                _context.PointTransactions.Add(new PointTransaction
                {
                    LoyaltyID = loyalty.LoyaltyID,
                    Points = -reward.PointsRequired,
                    Type = PointTransactionType.Redeem,
                    RefBookingID = booking.BookingID,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (promotion != null && customer != null)
            {
                _context.CustomerPromotions.Add(new CustomerPromotion
                {
                    CustomerID = customer.CustomerID,
                    PromotionID = promotion.PromotionID,
                    BookingID = booking.BookingID,
                    UsedAt = DateTime.UtcNow,
                    DiscountAmountActual = promotionDiscount
                });
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("[BR-33] Booking created BookingID={BookingId}, CustomerID={CustomerId}, Amount={Amount}",
                booking.BookingID, customerId ?? 0, booking.FinalAmount);

            // Đẩy real-time cho Admin đang online — thay cho việc FE phải polling mỗi 5 giây để phát hiện booking mới.
            if (_adminNotifier != null)
            {
                await _adminNotifier.NotifyNewBookingAsync(booking.BookingID, customer?.FullName ?? phone, licensePlate, service.ServiceName);
            }

            // BR-Realtime: đẩy tình trạng khung giờ cho toàn bộ client đang xem trang đặt lịch.
            // overlapCount là số booking trùng slot TRƯỚC khi thêm booking này, nên +1 để có số hiện tại.
            if (_bookingHubNotifier != null)
            {
                var occupiedCount = Math.Min(maxParallelSlots, overlapCount + 1);
                var availableCount = Math.Max(0, maxParallelSlots - occupiedCount);
                var slotLocal = newStart.AddHours(7);
                await _bookingHubNotifier.NotifySlotOccupancyChangedAsync(
                    slotLocal.ToString("yyyy-MM-dd"),
                    slotLocal.ToString("HH:mm"),
                    availableCount,
                    availableCount > 0 ? "Available" : "Full");
            }

            return new BookingResponseDto
            {
                BookingId = booking.BookingID,
                Phone = booking.Phone,
                LicensePlate = booking.LicensePlate,
                Service = new ServiceResponse
                {
                    ServiceId = service.ServiceID,
                    ServiceName = service.ServiceName,
                    Duration = service.Duration,
                    Price = service.Price,
                    ServiceCategory = service.ServiceCategory,
                    Status = service.Status,
                    Description = service.Description
                },
                ScheduledTime = booking.ScheduledTime,
                Status = booking.Status.ToString(),
                Invoice = new InvoiceResponseDto
                {
                    BaseAmount = baseAmount,
                    TierDiscount = tierDiscount,
                    RewardDiscount = rewardDiscount,
                    PromotionDiscount = promotionDiscount,
                    DiscountApplied = discountApplied,
                    FinalAmount = finalAmount
                },
                FinalAmount = booking.FinalAmount,
                PointsEarned = booking.PointsEarned,
                CreatedAt = booking.CreatedAt
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

            var bookings = await (from b in query
                                  join s in _context.Services on b.ServiceID equals s.ServiceID into sj
                                  from svc in sj.DefaultIfEmpty()
                                  orderby b.ScheduledTime descending
                                  select new BookingResponseDto
                                  {
                                      BookingId = b.BookingID,
                                      LicensePlate = b.LicensePlate,
                                      ScheduledTime = b.ScheduledTime,
                                      Status = b.Status.ToString(),
                                      FinalAmount = b.FinalAmount,
                                      PointsEarned = b.PointsEarned,
                                      Service = svc == null ? new ServiceResponse() : new ServiceResponse
                                      {
                                          ServiceId = svc.ServiceID,
                                          ServiceName = svc.ServiceName,
                                          Price = svc.Price,
                                          Duration = svc.Duration,
                                          Description = svc.Description,
                                          Status = svc.Status,
                                      },
                                  })
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResponse<BookingResponseDto>
            {
                Page = page,
                Total = total,
                PageSize = pageSize,
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

            int pointsRefunded = booking.PointsRedeemed;
            if (pointsRefunded > 0 && booking.CustomerID > 0)
            {
                var loyalty = await _context.LoyaltyAccounts
                    .FirstOrDefaultAsync(l => l.CustomerID == booking.CustomerID);
                if (loyalty != null)
                {
                    loyalty.TotalPoints += pointsRefunded;
                    loyalty.LastUpdated = DateTime.UtcNow;

                    _context.PointTransactions.Add(new PointTransaction
                    {
                        LoyaltyID = loyalty.LoyaltyID,
                        Points = pointsRefunded,
                        Type = PointTransactionType.Earn, // Earn represents positive points change
                        RefBookingID = booking.BookingID,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();

            // BR-Realtime: hủy lịch giải phóng 1 chỗ trong slot — đẩy lại số chỗ trống hiện tại
            // cho toàn bộ client đang xem trang đặt lịch.
            if (_bookingHubNotifier != null)
            {
                var slotService = await _context.Services.FirstOrDefaultAsync(s => s.ServiceID == booking.ServiceID);
                var duration = slotService?.Duration ?? 0;
                var slotStart = booking.ScheduledTime;
                var slotEnd = slotStart.AddMinutes(duration + 5);

                var activeBookings = await _context.Bookings
                    .Where(b => b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.Failed && b.Status != BookingStatus.NoShow)
                    .Where(b => b.ScheduledTime.Date == slotStart.Date)
                    .Select(b => new
                    {
                        b.ScheduledTime,
                        Duration = _context.Services.Where(s => s.ServiceID == b.ServiceID).Select(s => s.Duration).FirstOrDefault()
                    })
                    .ToListAsync();

                int overlapCount = 0;
                foreach (var b in activeBookings)
                {
                    var bStart = b.ScheduledTime;
                    var bEnd = bStart.AddMinutes(b.Duration + 5);
                    if (bStart < slotEnd && slotStart < bEnd) overlapCount++;
                }

                int maxParallelSlots = _bookingSettings.MaxParallelSlots > 0 ? _bookingSettings.MaxParallelSlots : 1;
                var availableCount = Math.Max(0, maxParallelSlots - overlapCount);
                var slotLocal = slotStart.AddHours(7);
                await _bookingHubNotifier.NotifySlotOccupancyChangedAsync(
                    slotLocal.ToString("yyyy-MM-dd"),
                    slotLocal.ToString("HH:mm"),
                    availableCount,
                    availableCount > 0 ? "Available" : "Full");
            }

            return new CancelBookingResponseDto
            {
                BookingId = booking.BookingID,
                Status = "Cancelled",
                PointsRefunded = pointsRefunded,
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

                    if (_pointService != null)
                    {
                        await _pointService.EarnPointsAsync(booking.BookingID);
                    }

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

        public async Task<IEnumerable<AvailableSlotResponse>> GetAvailableSlotsAsync(int? customerId, string? dateStr, string? licensePlate)
        {
            var windowDays = 7;
            bool isPriorityEligible = false;
            if (customerId.HasValue && customerId.Value > 0)
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.CustomerID == customerId.Value);
                if (customer != null)
                {
                    var tier = await _context.Tiers.FirstOrDefaultAsync(t => t.TierID == customer.TierID);
                    if (tier != null)
                    {
                        windowDays = tier.BookingWindowDays;

                        // BR-19: giữ nhất quán với CreateBookingAsync — khách hạng cao hơn mức thấp nhất
                        // vẫn được tính thêm buffer ưu tiên khi xét slot còn trống.
                        var basePriorityScore = await _context.Tiers.MinAsync(t => (int?)t.PriorityScore) ?? 1;
                        isPriorityEligible = tier.PriorityScore > basePriorityScore;
                    }
                }
            }

            var currentUtc = DateTime.UtcNow;
            var todayLocal = currentUtc.AddHours(7).Date;

            var datesToGenerate = new List<DateTime>();
            for (int i = 0; i < windowDays; i++)
            {
                datesToGenerate.Add(todayLocal.AddDays(i));
            }

            if (!string.IsNullOrEmpty(dateStr))
            {
                if (DateTime.TryParse(dateStr, out var requestedDate))
                {
                    datesToGenerate = datesToGenerate.Where(d => d.Date == requestedDate.Date).ToList();
                }
            }

            var minDateUtc = todayLocal.AddHours(-7);
            var maxDateUtc = todayLocal.AddDays(windowDays).AddHours(24 - 7);

            var activeBookings = await _context.Bookings
                .Where(b => (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.InProgress || b.Status == BookingStatus.Completed)
                         && b.ScheduledTime >= minDateUtc
                         && b.ScheduledTime <= maxDateUtc)
                .Select(b => new
                {
                    b.ScheduledTime,
                    Duration = _context.Services.Where(s => s.ServiceID == b.ServiceID).Select(s => s.Duration).FirstOrDefault(),
                    b.LicensePlate,
                    b.Status
                })
                .ToListAsync();

            var result = new List<AvailableSlotResponse>();

            foreach (var date in datesToGenerate)
            {
                var response = new AvailableSlotResponse { Date = date.ToString("yyyy-MM-dd"), Slots = new List<TimeSlotDto>() };
                var startTime = date.AddHours(7).AddMinutes(30); // 07:30 local
                var endTime = date.AddHours(19).AddMinutes(30); // 19:30 local

                for (var slotLocal = startTime; slotLocal <= endTime; slotLocal = slotLocal.AddMinutes(60))
                {
                    var slotUtc = slotLocal.AddHours(-7);

                    bool isAvailable = true;
                    int maxParallelSlots = _bookingSettings.MaxParallelSlots > 0 ? _bookingSettings.MaxParallelSlots : 1;
                    int availableCount = maxParallelSlots;

                    // BR-29: Advance Notice 60 mins
                    if (slotUtc < currentUtc.AddMinutes(60))
                    {
                        isAvailable = false;
                        availableCount = 0;
                    }
                    else
                    {
                        int overlapCount = 0;
                        bool isLicensePlateViolated = false;

                        foreach (var b in activeBookings)
                        {
                            // BUG FIX: b.ScheduledTime được lưu dưới dạng UTC (giống mọi nơi khác
                            // trong hệ thống), nhưng biến này bị gắn nhãn "Local" mà không cộng offset
                            // +7h — khiến so sánh với slotLocal (giờ VN thật) bị lệch 7 tiếng, làm slot
                            // đã đặt hiển thị nhầm là còn trống (và một slot khác, không ai đặt, lại bị
                            // đánh dấu nhầm là đầy).
                            var bStartLocal = b.ScheduledTime.Kind == DateTimeKind.Utc ? b.ScheduledTime.AddHours(7) : b.ScheduledTime;
                            var bEndLocal = bStartLocal.AddMinutes(b.Duration + 5);

                            // Overlap
                            if (slotLocal >= bStartLocal && slotLocal < bEndLocal)
                            {
                                overlapCount++;
                            }

                            // BR-28: Same license plate < 120 mins (ONLY for active non-completed bookings)
                            if (!string.IsNullOrEmpty(licensePlate) && !string.IsNullOrEmpty(b.LicensePlate) && b.Status != BookingStatus.Completed)
                            {
                                if (b.LicensePlate.Equals(licensePlate, StringComparison.OrdinalIgnoreCase))
                                {
                                    if (Math.Abs((slotLocal - bStartLocal).TotalMinutes) < 120)
                                    {
                                        isLicensePlateViolated = true;
                                    }
                                }
                            }
                        }

                        int priorityBuffer = _bookingSettings.PriorityBufferSlots > 0 ? _bookingSettings.PriorityBufferSlots : 0;
                        int effectiveCap = maxParallelSlots + (isPriorityEligible ? priorityBuffer : 0);

                        availableCount = Math.Max(0, effectiveCap - overlapCount);
                        if (overlapCount >= effectiveCap || isLicensePlateViolated)
                        {
                            isAvailable = false;
                            availableCount = 0;
                        }
                    }

                    response.Slots.Add(new TimeSlotDto
                    {
                        Time = slotLocal.ToString("HH:mm"),
                        IsAvailable = isAvailable,
                        AvailableCount = availableCount
                    });
                }

                result.Add(response);
            }

            return result;
        }
    }
}