using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AutoWash.Application.Interfaces;

namespace AutoWash.Infrastructure.Jobs
{
    // BackgroundService chạy hàng ngày lúc 00:00 UTC để expire điểm hết hạn (BR-57)
    public class PointExpiryJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PointExpiryJob> _logger;

        private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public PointExpiryJob(IServiceScopeFactory scopeFactory, ILogger<PointExpiryJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = GetDelayUntilMidnightUtc();
                _logger.LogInformation("[PointExpiryJob] Lần chạy tiếp theo sau {Hours:F1} giờ", delay.TotalHours);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (!await _lock.WaitAsync(0, stoppingToken))
                {
                    _logger.LogWarning("[PointExpiryJob] Bỏ qua — đang có lần chạy khác.");
                    continue;
                }

                try
                {
                    _logger.LogInformation("[PointExpiryJob] Bắt đầu expire điểm ngày {Date:yyyy-MM-dd}",
                        DateTime.UtcNow);

                    using var scope = _scopeFactory.CreateScope();
                    var pointService = scope.ServiceProvider.GetRequiredService<IPointService>();
                    await pointService.RunDailyExpiryAsync();

                    _logger.LogInformation("[PointExpiryJob] Hoàn tất expire điểm.");
                }
                finally
                {
                    _lock.Release();
                }
            }
        }

        private static TimeSpan GetDelayUntilMidnightUtc()
        {
            var now = DateTime.UtcNow;
            var nextMidnight = now.Date.AddDays(1); // ngày mai 00:00:00 UTC
            return nextMidnight - now;
        }
    }
}
