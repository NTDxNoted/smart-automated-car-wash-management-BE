using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AutoWash.Application.Interfaces;

namespace AutoWash.Infrastructure.Jobs
{
    // BackgroundService chạy mùng 1 hàng tháng (BR-22)
    public class TierDowngradeJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TierDowngradeJob> _logger;

        public TierDowngradeJob(IServiceScopeFactory scopeFactory, ILogger<TierDowngradeJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = GetDelayUntilFirstOfNextMonth();
                _logger.LogInformation("[TierDowngradeJob] Lần chạy tiếp theo sau {Hours:F1} giờ", delay.TotalHours);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                _logger.LogInformation("[TierDowngradeJob] Bắt đầu hạ hạng tháng {Month}/{Year}",
                    DateTime.UtcNow.Month, DateTime.UtcNow.Year);

                using var scope = _scopeFactory.CreateScope();
                var tierService = scope.ServiceProvider.GetRequiredService<ITierService>();
                await tierService.RunMonthlyDowngradeAsync();

                _logger.LogInformation("[TierDowngradeJob] Hoàn tất hạ hạng.");
            }
        }

        private static TimeSpan GetDelayUntilFirstOfNextMonth()
        {
            var now = DateTime.UtcNow;
            var nextRun = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
            return nextRun - now;
        }
    }
}
