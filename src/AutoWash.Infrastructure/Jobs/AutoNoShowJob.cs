using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AutoWash.Infrastructure.Jobs
{
    public class AutoNoShowJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AutoNoShowJob> _logger;

        public AutoNoShowJob(
            IServiceScopeFactory scopeFactory,
            ILogger<AutoNoShowJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var dbContext = scope.ServiceProvider
                        .GetRequiredService<IApplicationDbContext>();

                    // BUG FIX (ISSUE-20 comment was wrong): ScheduledTime được lưu dưới dạng UTC, giống
                    // mọi timestamp khác trong hệ thống (xem CreateBookingAsync/GetAvailableSlotsAsync) —
                    // không phải giờ VN. So với nowVietnam (giờ VN) khiến cutoff bị lệch sớm ~7 tiếng,
                    // đánh NoShow các booking Pending gần như ngay sau khi vừa tạo dù chưa tới giờ hẹn.
                    var cutoffTimeUtc = DateTime.UtcNow.AddMinutes(-15);

                    var overdueBookings = await dbContext.Bookings
                        .Where(b =>
                            b.Status == BookingStatus.Pending &&
                            b.ScheduledTime <= cutoffTimeUtc &&
                            b.CheckInTime == null)
                        .ToListAsync(stoppingToken);

                    if (overdueBookings.Any())
                    {
                        var now = DateTime.UtcNow;

                        foreach (var booking in overdueBookings)
                        {
                            booking.Status = BookingStatus.NoShow;
                            booking.CompletedAt = now;

                            _logger.LogWarning(
                                "Booking {BookingID} automatically marked as NoShow because customer did not show up.",
                                booking.BookingID);
                        }

                        // Đếm số no-show trong chính batch này (chưa lưu DB) theo từng khách, để cộng
                        // vào lịch sử — không SaveChanges giữa 2 bước để tránh mất trạng thái suspend
                        // nếu bước sau lỗi (booking đã NoShow nhưng không còn match filter Pending ở lần chạy sau).
                        string CustomerKey(int customerId, string? phone) =>
                            customerId > 0 ? customerId.ToString() : (phone ?? string.Empty);

                        var batchCountByCustomer = overdueBookings
                            .GroupBy(b => CustomerKey(b.CustomerID, b.Phone))
                            .ToDictionary(g => g.Key, g => g.Count());

                        var cutoff = now.AddDays(-30);

                        foreach (var booking in overdueBookings)
                        {
                            // c.Phone != "GUEST" loại trừ tài khoản khách vãng lai dùng chung (BookingService gán mọi
                            // booking guest vào 1 CustomerID) — nếu không, no-show của các khách lạ khác nhau sẽ bị
                            // cộng dồn và khóa nhầm tài khoản hệ thống dùng chung.
                            var customer = await dbContext.Customers.FirstOrDefaultAsync(c =>
                                (booking.CustomerID > 0 && c.CustomerID == booking.CustomerID && c.Phone != "GUEST") ||
                                (!string.IsNullOrWhiteSpace(booking.Phone) && c.Phone == booking.Phone), stoppingToken);

                            if (customer == null)
                            {
                                continue;
                            }

                            var historicalCount = await dbContext.Bookings.CountAsync(b =>
                                b.Status == BookingStatus.NoShow &&
                                ((b.CompletedAt ?? b.CreatedAt) >= cutoff) &&
                                ((b.CustomerID > 0 && b.CustomerID == customer.CustomerID) ||
                                 (!string.IsNullOrWhiteSpace(b.Phone) && b.Phone == customer.Phone)), stoppingToken);

                            var batchCount = batchCountByCustomer.TryGetValue(CustomerKey(customer.CustomerID, customer.Phone), out var c2) ? c2 : 0;
                            var noShowCount = historicalCount + batchCount;

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

                        await dbContext.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation(
                            "AutoNoShowJob processed {Count} overdue bookings.",
                            overdueBookings.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error occurred executing AutoNoShowJob.");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}