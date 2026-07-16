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

                    var cutoffTime = DateTime.UtcNow.AddMinutes(-15);

                    var overdueBookings = await dbContext.Bookings
                        .Where(b =>
                            b.Status == BookingStatus.Pending &&
                            b.ScheduledTime <= cutoffTime &&
                            b.CheckInTime == null)
                        .ToListAsync(stoppingToken);

                    if (overdueBookings.Any())
                    {
                        foreach (var booking in overdueBookings)
                        {
                            booking.Status = BookingStatus.NoShow;
                            booking.CompletedAt = DateTime.UtcNow;

                            _logger.LogWarning(
                                "Booking {BookingID} automatically marked as NoShow because customer did not show up.",
                                booking.BookingID);
                        }

                        await dbContext.SaveChangesAsync(stoppingToken);

                        foreach (var booking in overdueBookings)
                        {
                            var customer = await dbContext.Customers.FirstOrDefaultAsync(c =>
                                (booking.CustomerID > 0 && c.CustomerID == booking.CustomerID) ||
                                (!string.IsNullOrWhiteSpace(booking.Phone) && c.Phone == booking.Phone));

                            if (customer == null)
                            {
                                continue;
                            }

                            var now = DateTime.UtcNow;
                            var cutoff = now.AddDays(-30);
                            var noShowCount = 1 + await dbContext.Bookings.CountAsync(b =>
                                b.Status == BookingStatus.NoShow &&
                                ((b.CompletedAt ?? b.CreatedAt) >= cutoff) &&
                                ((b.CustomerID > 0 && b.CustomerID == customer.CustomerID) ||
                                 (!string.IsNullOrWhiteSpace(b.Phone) && b.Phone == customer.Phone)));

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