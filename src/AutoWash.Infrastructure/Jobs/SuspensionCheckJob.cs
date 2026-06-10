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
    public class SuspensionCheckJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SuspensionCheckJob> _logger;

        public SuspensionCheckJob(IServiceScopeFactory scopeFactory, ILogger<SuspensionCheckJob> logger)
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

                    var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

                    // Lấy những user có 3 lịch NoShow trong 30 ngày qua và chưa bị phạt
                    var violators = await dbContext.Bookings
                        .Where(b => b.Status == BookingStatus.NoShow && b.ScheduledTime >= thirtyDaysAgo)
                        .GroupBy(b => b.CustomerID)
                        .Select(g => new { CustomerID = g.Key, NoShowCount = g.Count() })
                        .Where(g => g.NoShowCount >= 3)
                        .ToListAsync(stoppingToken);

                    foreach (var violator in violators)
                    {
                        var customer = await dbContext.Customers.FindAsync(new object[] { violator.CustomerID }, stoppingToken);
                        if (customer != null && (customer.SuspendedUntil == null || customer.SuspendedUntil < DateTime.UtcNow))
                        {
                            customer.SuspendedUntil = DateTime.UtcNow.AddDays(15);
                            _logger.LogWarning("Customer {CustomerID} suspended until {SuspendedUntil} due to 3 No-Shows in the last 30 days.", customer.CustomerID, customer.SuspendedUntil);
                        }
                    }

                    await dbContext.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing SuspensionCheckJob.");
                }

                // Chạy mỗi 10 phút để tránh nặng DB
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }
    }
}
