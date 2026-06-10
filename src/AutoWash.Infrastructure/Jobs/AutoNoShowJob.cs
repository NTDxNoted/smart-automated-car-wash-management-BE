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

        public AutoNoShowJob(IServiceScopeFactory scopeFactory, ILogger<AutoNoShowJob> logger)
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
                    var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                    var cutoffTime = DateTime.UtcNow.AddMinutes(-15);

                    var overdueBookings = await dbContext.Bookings
                        .Where(b => b.Status == BookingStatus.Pending 
                                    && b.ScheduledTime <= cutoffTime 
                                    && b.CheckInTime == null)
                        .ToListAsync(stoppingToken);

                    if (overdueBookings.Any())
                    {
                        var affectedCustomerIds = overdueBookings.Select(b => b.CustomerID).Distinct().ToList();

                        foreach (var booking in overdueBookings)
                        {
                            booking.Status = BookingStatus.NoShow;
                            booking.CompletedAt = DateTime.UtcNow;
                            _logger.LogWarning("Booking {BookingID} automatically marked as NoShow.", booking.BookingID);
                        }

                        // Lưu trạng thái NoShow trước
                        await dbContext.SaveChangesAsync(stoppingToken);

                        // Kích hoạt ngay lập tức check Suspension (BR-66) cho các Customer vừa bị NoShow
                        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                        
                        // Truy vấn tổng số NoShow trong 30 ngày (dựa theo ScheduledTime) của những khách hàng này
                        var violatorCounts = await dbContext.Bookings
                            .Where(b => affectedCustomerIds.Contains(b.CustomerID)
                                        && b.Status == BookingStatus.NoShow 
                                        && b.ScheduledTime >= thirtyDaysAgo)
                            .GroupBy(b => b.CustomerID)
                            .Select(g => new { CustomerID = g.Key, NoShowCount = g.Count() })
                            .ToListAsync(stoppingToken);

                        foreach (var stat in violatorCounts)
                        {
                            if (stat.NoShowCount >= 3)
                            {
                                var customer = await dbContext.Customers.FindAsync(new object[] { stat.CustomerID }, stoppingToken);
                                if (customer != null && (customer.SuspendedUntil == null || customer.SuspendedUntil < DateTime.UtcNow))
                                {
                                    customer.SuspendedUntil = DateTime.UtcNow.AddDays(15);
                                    _logger.LogWarning("BR-66 TRIGGERED: Customer {CustomerID} suspended until {SuspendedUntil} due to {Count} No-Shows in the last 30 days.", customer.CustomerID, customer.SuspendedUntil, stat.NoShowCount);
                                }
                            }
                        }

                        await dbContext.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing AutoNoShowJob.");
                }

                // Chạy mỗi 1 phút
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
