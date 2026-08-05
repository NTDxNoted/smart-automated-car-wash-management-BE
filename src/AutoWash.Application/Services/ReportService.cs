using AutoWash.Application.DTOs;
using AutoWash.Application.DTOs.Admin;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Entities;
using AutoWash.Domain.Enums;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AutoWash.Application.Services
{
  public class ReportService : IReportService
  {
    private readonly IApplicationDbContext _context;
    private readonly BookingSettings _bookingSettings;

    public ReportService(IApplicationDbContext context, IOptions<BookingSettings>? bookingSettings = null)
    {
      _context = context;
      _bookingSettings = bookingSettings?.Value ?? new BookingSettings { MaxParallelSlots = 3 };
    }

    // Shared by GetOverviewReportAsync and GetCompletionRateDetailAsync so both endpoints resolve
    // the same [start, end) window from filterType/startDate/endDate and never drift apart.
    private static (DateTime Start, DateTime End) ResolveDateRangeUtc(string? filterType, DateTime? startDate, DateTime? endDate)
    {
      var now = DateTime.UtcNow;
      DateTime start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
      DateTime end = start.AddMonths(1);

      if (!string.IsNullOrEmpty(filterType))
      {
          switch (filterType.ToLower())
          {
              case "day":
                  start = DateTime.UtcNow.Date;
                  end = start.AddDays(1);
                  break;
              case "week":
                  int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
                  start = now.AddDays(-1 * diff).Date;
                  end = start.AddDays(7);
                  break;
              case "month":
                  start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                  end = start.AddMonths(1);
                  break;
              case "year":
                  start = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                  end = start.AddYears(1);
                  break;
          }
      }
      else if (startDate.HasValue && endDate.HasValue)
      {
          start = startDate.Value.Date;
          end = endDate.Value.Date.AddDays(1);
      }

      return (start, end);
    }

    public async Task<OverviewReportResponse> GetOverviewReportAsync(string filterType, DateTime? startDate, DateTime? endDate)
    {
      var (start, end) = ResolveDateRangeUtc(filterType, startDate, endDate);

      var bookings = await _context.Bookings
          .Where(b => b.CreatedAt >= start && b.CreatedAt < end)
          .ToListAsync();

      var totalBookings = bookings.Count;
      var completedBookings = bookings.Count(b => b.Status == BookingStatus.Completed);
      var failedBookings = bookings.Count(b => b.Status == BookingStatus.Failed);
      var noShowBookings = bookings.Count(b => b.Status == BookingStatus.NoShow);
      var cancelledBookings = bookings.Count(b => b.Status == BookingStatus.Cancelled);
      var totalRevenue = bookings.Where(b => b.Status == BookingStatus.Completed).Sum(b => b.FinalAmount);

      return new OverviewReportResponse
      {
        Period = filterType ?? "custom",
        TotalBookings = totalBookings,
        CompletedBookings = completedBookings,
        FailedBookings = failedBookings,
        NoShowBookings = noShowBookings,
        CancelledBookings = cancelledBookings,
        TotalRevenue = totalRevenue,
        NoShowRate = totalBookings == 0 ? 0m : Math.Round((decimal)noShowBookings / totalBookings, 4),
        CompletionRate = totalBookings == 0 ? 0m : Math.Round((decimal)completedBookings / totalBookings, 4),
        AvgOrderValue = totalBookings == 0 ? 0m : Math.Round(totalRevenue / totalBookings, 2)
      };
    }

    public async Task<IReadOnlyList<AutoWash.Application.DTOs.Admin.PopularServiceResponse>> GetPopularServicesReportAsync(DateTime? startDate, DateTime? endDate)
    {
      var query = _context.Bookings
          .Where(b => b.Status == BookingStatus.Completed);

      if (startDate.HasValue)
      {
          query = query.Where(b => b.CreatedAt >= startDate.Value.Date);
      }
      if (endDate.HasValue)
      {
          query = query.Where(b => b.CreatedAt < endDate.Value.Date.AddDays(1));
      }

      var completedBookings = await query.ToListAsync();
      var totalCount = completedBookings.Count;

      var services = await _context.Services.ToListAsync();

      var result = completedBookings
          .GroupBy(b => b.ServiceID)
          .Select(g => {
              var service = services.FirstOrDefault(s => s.ServiceID == g.Key);
              var serviceName = service?.ServiceName ?? "Dịch vụ không xác định";
              if (service?.Status == "Deleted")
              {
                  serviceName = $"{serviceName} (Đã xóa)";
              }
              return new AutoWash.Application.DTOs.Admin.PopularServiceResponse
              {
                  ServiceId = g.Key,
                  ServiceName = serviceName,
                  UsageCount = g.Count(),
                  TotalRevenue = g.Sum(b => b.FinalAmount),
                  Percentage = totalCount == 0 ? 0m : Math.Round((decimal)g.Count() * 100m / totalCount, 2)
              };
          })
          .OrderByDescending(x => x.UsageCount)
          .ToList();

      return result.AsReadOnly();
    }

    public async Task<IReadOnlyList<RfmReportResponse>> GetRfmReportAsync()
    {
      var rows = await _context.VwCustomerRfm
          .AsNoTracking()
          .Select(x => new RfmReportResponse
          {
            CustomerId = x.CustomerId,
            FullName = x.FullName,
            Phone = x.Phone,
            CurrentTier = x.CurrentTier,
            RecencyDays = x.RecencyDays,
            Frequency = x.Frequency,
            MonetaryTotal = x.MonetaryTotal,
            TotalPoints = x.TotalPoints,
            TotalSpending = x.TotalSpending,
            MemberSince = x.MemberSince
          })
          .OrderByDescending(x => x.MonetaryTotal)
          .ToListAsync();

      return rows;
    }

    public async Task<IReadOnlyList<TierDistributionResponse>> GetTierDistributionAsync()
    {
      // Tier thật của khách là Customer.TierID (qua bảng Tiers, admin-configurable),
      // không phải suy ra từ điểm thưởng — 2 trục độc lập, xem CustomerService.GetProfileAsync.
      var tierNames = await _context.Tiers.ToDictionaryAsync(t => t.TierID, t => t.TierName);
      var customers = await _context.Customers.ToListAsync();

      var totalCustomers = customers.Count;
      var distribution = customers
          .GroupBy(c => tierNames.TryGetValue(c.TierID, out var name) ? name : (c.TierID == 1 ? "Member" : c.TierID.ToString()))
          .Select(g => new TierDistributionResponse
          {
            Tier = g.Key,
            CustomerCount = g.Count(),
            Percentage = totalCustomers == 0 ? 0m : Math.Round((decimal)g.Count() * 100m / totalCustomers, 2)
          })
          .OrderByDescending(x => x.CustomerCount)
          .ToList();

      return distribution;
    }

    public async Task<LoyaltyStatsResponse> GetLoyaltyStatsAsync()
    {
      var loyaltyAccounts = await _context.LoyaltyAccounts
          .Include(x => x.PointTransactions)
          .AsNoTracking()
          .ToListAsync();

      var totalPoints = loyaltyAccounts.Sum(x => x.TotalPoints);
      var expiringSoon = loyaltyAccounts
          .SelectMany(x => x.PointTransactions.Where(pt => pt.ExpiredAt.HasValue && pt.ExpiredAt.Value >= DateTime.UtcNow && pt.ExpiredAt.Value <= DateTime.UtcNow.AddDays(7)))
          .Sum(pt => pt.Points);
      var expired = loyaltyAccounts
          .SelectMany(x => x.PointTransactions.Where(pt => pt.ExpiredAt.HasValue && pt.ExpiredAt.Value < DateTime.UtcNow))
          .Sum(pt => pt.Points);

      return new LoyaltyStatsResponse
      {
        TotalPointsInCirculation = totalPoints,
        PointsExpiringSoon = expiringSoon,
        ExpiredPoints = expired
      };
    }

    // Vietnam is UTC+7 with no DST — fixed offset is safe.
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    public async Task<PeakOccupancyResponse> GetPeakOccupancyReportAsync(DateTime startDate, DateTime endDate)
    {
      int maxParallelSlots = _bookingSettings.MaxParallelSlots > 0 ? _bookingSettings.MaxParallelSlots : 1;
      var eligibleStatuses = new[] { BookingStatus.Completed, BookingStatus.Pending };

      // ScheduledTime lưu dưới dạng UTC; dịch biên ngày VN sang UTC trước khi so sánh, giống
      // GetPromotionRoiReportAsync bên dưới — nếu không, booking VN 00:00-07:00 bị loại và booking
      // sáng sớm hôm sau lại bị tính nhầm vào ngày trước.
      var rangeStart = startDate.Date.AddHours(-7);
      var rangeEnd = endDate.Date.AddDays(1).AddHours(-7);
      var totalDays = (endDate.Date - startDate.Date).Days + 1;

      var bookings = await _context.Bookings
          .Where(b => b.ScheduledTime >= rangeStart
                   && b.ScheduledTime < rangeEnd
                   && eligibleStatuses.Contains(b.Status))
          .ToListAsync();

      var dayOrder = new[]
      {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
        DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
      };

      var dayStats = dayOrder
          .Select(day => new DayOfWeekStatDto
          {
            DayOfWeek = day.ToString(),
            BookingCount = bookings.Count(b => b.ScheduledTime.Add(VietnamOffset).DayOfWeek == day)
          })
          .ToList();

      var hourStats = new List<HourStatDto>();
      var slotStart = new TimeSpan(7, 30, 0);
      var slotEnd = new TimeSpan(17, 30, 0);
      var current = slotStart;

      while (current <= slotEnd)
      {
        var next = current.Add(TimeSpan.FromMinutes(30));
        var count = bookings.Count(b =>
        {
          var t = b.ScheduledTime.Add(VietnamOffset).TimeOfDay;
          return t >= current && t < next;
        });

        var denominator = totalDays * maxParallelSlots;
        var occupancy = denominator == 0 ? 0m
            : Math.Round((decimal)count / denominator * 100, 2);

        hourStats.Add(new HourStatDto
        {
          TimeSlot = $"{(int)current.TotalHours:D2}:{current.Minutes:D2}",
          BookingCount = count,
          OccupancyPercentage = occupancy
        });

        current = next;
      }

      return new PeakOccupancyResponse
      {
        StartDate = startDate.ToString("yyyy-MM-dd"),
        EndDate = endDate.ToString("yyyy-MM-dd"),
        TotalDays = totalDays,
        MaxParallelSlots = maxParallelSlots,
        DayOfWeekStats = dayStats,
        HourStats = hourStats
      };
    }

    public async Task<PromotionRoiResponse> GetPromotionRoiReportAsync(DateTime startDate, DateTime endDate)
    {
      // CompletedAt is stored as UTC; shift VN date boundaries to UTC before comparing
      var rangeStart = startDate.Date.AddHours(-7);
      var rangeEnd = endDate.Date.AddDays(1).AddHours(-7);

      var rawItems = await (
          from cp in _context.CustomerPromotions
          join p in _context.Promotions on cp.PromotionID equals p.PromotionID
          join b in _context.Bookings on cp.BookingID equals b.BookingID
          join t in _context.Transactions on b.BookingID equals t.BookingID
          where b.Status == BookingStatus.Completed
             && t.Status == TransactionStatus.Paid
             && b.CompletedAt >= rangeStart
             && b.CompletedAt < rangeEnd
          group new { cp.DiscountAmountActual, b.FinalAmount }
            by new { p.PromotionID, p.Title, p.PromoCode } into g
          select new
          {
            g.Key.PromotionID,
            g.Key.Title,
            g.Key.PromoCode,
            UsageCount = g.Count(),
            TotalDiscountGiven = g.Sum(x => x.DiscountAmountActual),
            RevenueGenerated = g.Sum(x => x.FinalAmount)
          }
      ).ToListAsync();

      var items = rawItems
          .OrderByDescending(x => x.RevenueGenerated)
          .Select(x => new PromotionRoiItemDto
          {
            PromotionId = x.PromotionID,
            Title = x.Title,
            PromoCode = x.PromoCode,
            UsageCount = x.UsageCount,
            TotalDiscountGiven = x.TotalDiscountGiven,
            RevenueGenerated = x.RevenueGenerated,
            RoiPercentage = x.TotalDiscountGiven == 0 ? 0m
                : Math.Round((x.RevenueGenerated - x.TotalDiscountGiven) / x.TotalDiscountGiven * 100, 2)
          })
          .ToList();

      return new PromotionRoiResponse
      {
        StartDate = startDate.ToString("yyyy-MM-dd"),
        EndDate = endDate.ToString("yyyy-MM-dd"),
        TotalPromotions = items.Count,
        Items = items
      };
    }

    public async Task<RevenueDetailResponse> GetRevenueDetailReportAsync(DateTime startDate, DateTime endDate, string? paymentMethod)
    {
      // PaidAt is stored as UTC; shift VN date boundaries to UTC before comparing, same as
      // GetPromotionRoiReportAsync above.
      var rangeStart = startDate.Date.AddHours(-7);
      var rangeEnd = endDate.Date.AddDays(1).AddHours(-7);

      PaymentMethod? methodFilter = null;
      if (!string.IsNullOrWhiteSpace(paymentMethod) && !paymentMethod.Equals("all", StringComparison.OrdinalIgnoreCase))
      {
        if (!Enum.TryParse<PaymentMethod>(paymentMethod, true, out var parsed))
          throw new ArgumentException("Phương thức thanh toán không hợp lệ.");
        methodFilter = parsed;
      }

      var query =
          from t in _context.Transactions
          join b in _context.Bookings on t.BookingID equals b.BookingID
          where b.Status == BookingStatus.Completed
             && t.Status == TransactionStatus.Paid
             && t.PaidAt != null
             && t.PaidAt >= rangeStart
             && t.PaidAt < rangeEnd
          select new { Transaction = t, Booking = b };

      if (methodFilter.HasValue)
      {
        query = query.Where(x => x.Transaction.PaymentMethod == methodFilter.Value);
      }

      var rows = await query.ToListAsync();

      var customerNames = await _context.Customers.ToDictionaryAsync(c => c.CustomerID, c => c.FullName);
      var serviceNames = await _context.Services.ToDictionaryAsync(s => s.ServiceID, s => s.ServiceName);
      var promotions = await _context.Promotions.ToDictionaryAsync(p => p.PromotionID, p => new { p.Title, p.PromoCode });
      var rewardNames = await _context.RewardsCatalog.ToDictionaryAsync(r => r.RewardID, r => r.RewardName);

      var transactions = rows
          .OrderByDescending(x => x.Transaction.PaidAt)
          .Select(x =>
          {
            string? promotionApplied = null;
            if (x.Booking.PromotionID.HasValue && promotions.TryGetValue(x.Booking.PromotionID.Value, out var promo))
            {
              promotionApplied = string.IsNullOrWhiteSpace(promo.PromoCode) ? promo.Title : $"{promo.Title} ({promo.PromoCode})";
            }
            else if (x.Booking.RewardID.HasValue && rewardNames.TryGetValue(x.Booking.RewardID.Value, out var rewardName))
            {
              promotionApplied = rewardName;
            }

            return new RevenueTransactionItemDto
            {
              InvoiceCode = $"HD{x.Transaction.TransactionID:D5}",
              CustomerName = customerNames.TryGetValue(x.Booking.CustomerID, out var name) && !string.IsNullOrWhiteSpace(name)
                  ? name
                  : (string.IsNullOrWhiteSpace(x.Booking.Phone) ? "Khách vãng lai" : x.Booking.Phone),
              ServiceName = serviceNames.TryGetValue(x.Booking.ServiceID, out var svcName) ? svcName : "Dịch vụ không xác định",
              PaymentMethod = x.Transaction.PaymentMethod.ToString(),
              PromotionApplied = promotionApplied,
              DiscountAmount = x.Booking.DiscountApplied,
              Amount = x.Transaction.Amount,
              PaidAt = x.Transaction.PaidAt!.Value
            };
          })
          .ToList();

      return new RevenueDetailResponse
      {
        StartDate = startDate.ToString("yyyy-MM-dd"),
        EndDate = endDate.ToString("yyyy-MM-dd"),
        PaymentMethodFilter = methodFilter?.ToString() ?? "All",
        GrossRevenue = rows.Sum(x => x.Booking.BaseAmount),
        TotalDiscount = rows.Sum(x => x.Booking.DiscountApplied),
        NetRevenue = rows.Sum(x => x.Transaction.Amount),
        CashRevenue = rows.Where(x => x.Transaction.PaymentMethod == PaymentMethod.Cash).Sum(x => x.Transaction.Amount),
        TransferRevenue = rows.Where(x => x.Transaction.PaymentMethod == PaymentMethod.Transfer).Sum(x => x.Transaction.Amount),
        Transactions = transactions
      };
    }

    public async Task<CompletionRateDetailResponse> GetCompletionRateDetailAsync(string? filterType, DateTime? startDate, DateTime? endDate, int? serviceId, string? groupBy)
    {
      var (start, end) = ResolveDateRangeUtc(filterType, startDate, endDate);
      var windowLength = end - start;
      var previousStart = start - windowLength;
      var previousEnd = start;

      var bookings = await FilteredBookingsAsync(start, end, serviceId);
      var previousBookings = await FilteredBookingsAsync(previousStart, previousEnd, serviceId);

      var totalBookings = bookings.Count;
      var completedBookings = bookings.Count(b => b.Status == BookingStatus.Completed);
      var cancelledBookings = bookings.Count(b => b.Status == BookingStatus.Cancelled);
      var noShowBookings = bookings.Count(b => b.Status == BookingStatus.NoShow);
      var failedBookings = bookings.Count(b => b.Status == BookingStatus.Failed);
      var pendingBookings = bookings.Count(b => b.Status == BookingStatus.Pending && !b.CheckInTime.HasValue);
      var processingBookings = bookings.Count(b => b.Status == BookingStatus.Pending && b.CheckInTime.HasValue);
      var completionRate = totalBookings == 0 ? 0m : Math.Round((decimal)completedBookings / totalBookings, 4);

      var overview = new CompletionOverviewDto
      {
        TotalBookings = totalBookings,
        CompletedBookings = completedBookings,
        CancelledBookings = cancelledBookings,
        NoShowBookings = noShowBookings,
        PendingBookings = pendingBookings,
        ProcessingBookings = processingBookings,
        FailedBookings = failedBookings,
        CompletionRate = completionRate
      };

      var services = await _context.Services.ToListAsync();
      var topServices = BuildTopServices(bookings, services);
      var timeSlots = BuildTimeSlots(bookings);

      var previousCompletionRate = previousBookings.Count == 0 ? 0m
          : Math.Round((decimal)previousBookings.Count(b => b.Status == BookingStatus.Completed) / previousBookings.Count, 4);
      var previousCancelledBookings = previousBookings.Count(b => b.Status == BookingStatus.Cancelled);

      var periodComparison = new PeriodComparisonDto
      {
        CurrentCompletionRate = Math.Round(completionRate * 100, 2),
        PreviousCompletionRate = Math.Round(previousCompletionRate * 100, 2),
        DeltaPercentagePoints = Math.Round((completionRate - previousCompletionRate) * 100, 2),
        TrendDirection = completionRate > previousCompletionRate ? "Up" : completionRate < previousCompletionRate ? "Down" : "Flat"
      };

      return new CompletionRateDetailResponse
      {
        StartDate = start.ToString("yyyy-MM-dd"),
        EndDate = end.AddDays(-1).ToString("yyyy-MM-dd"),
        FilterType = filterType ?? "custom",
        GroupBy = string.IsNullOrWhiteSpace(groupBy) ? "day" : groupBy.ToLower(),
        Overview = overview,
        Formula = new CompletionRateFormulaDto
        {
          CompletedBookings = completedBookings,
          TotalBookings = totalBookings,
          ResultPercentage = Math.Round(completionRate * 100, 2)
        },
        StatusBreakdown = BuildStatusBreakdown(overview),
        Trend = BuildTrend(bookings, groupBy),
        FailureReasons = BuildFailureReasons(cancelledBookings, noShowBookings, failedBookings),
        TopServices = topServices,
        TimeSlots = timeSlots,
        Alerts = BuildAlerts(overview, cancelledBookings, previousCancelledBookings, topServices),
        PeriodComparison = periodComparison,
        Kpi = BuildKpi(Math.Round(completionRate * 100, 2)),
        Insights = BuildInsights(overview, periodComparison, topServices, timeSlots)
      };
    }

    public async Task<UnfinishedBookingsPageDto> GetUnfinishedBookingsAsync(string? filterType, DateTime? startDate, DateTime? endDate, int? serviceId, int page, int pageSize)
    {
      var (start, end) = ResolveDateRangeUtc(filterType, startDate, endDate);

      var query = _context.Bookings.Where(b => b.CreatedAt >= start && b.CreatedAt < end && b.Status != BookingStatus.Completed);
      if (serviceId.HasValue)
      {
        query = query.Where(b => b.ServiceID == serviceId.Value);
      }

      var totalCount = await query.CountAsync();

      var safePage = page < 1 ? 1 : page;
      var safePageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 200);

      var pageBookings = await query
          .OrderByDescending(b => b.ScheduledTime)
          .Skip((safePage - 1) * safePageSize)
          .Take(safePageSize)
          .ToListAsync();

      return new UnfinishedBookingsPageDto
      {
        Page = safePage,
        PageSize = safePageSize,
        TotalCount = totalCount,
        Items = await MapUnfinishedBookingsAsync(pageBookings)
      };
    }

    public async Task<byte[]> ExportUnfinishedBookingsAsync(string? filterType, DateTime? startDate, DateTime? endDate, int? serviceId)
    {
      const int maxRows = 5000;
      var (start, end) = ResolveDateRangeUtc(filterType, startDate, endDate);

      var query = _context.Bookings.Where(b => b.CreatedAt >= start && b.CreatedAt < end && b.Status != BookingStatus.Completed);
      if (serviceId.HasValue)
      {
        query = query.Where(b => b.ServiceID == serviceId.Value);
      }

      var bookings = await query
          .OrderByDescending(b => b.ScheduledTime)
          .Take(maxRows)
          .ToListAsync();

      var items = await MapUnfinishedBookingsAsync(bookings);

      using var workbook = new XLWorkbook();
      var sheet = workbook.Worksheets.Add("Booking chua hoan thanh");

      var headers = new[] { "Mã booking", "Khách hàng", "Số điện thoại", "Dịch vụ", "Thời gian hẹn", "Trạng thái" };
      for (var i = 0; i < headers.Length; i++)
      {
        sheet.Cell(1, i + 1).Value = headers[i];
        sheet.Cell(1, i + 1).Style.Font.Bold = true;
      }

      for (var i = 0; i < items.Count; i++)
      {
        var item = items[i];
        var row = i + 2;
        sheet.Cell(row, 1).Value = item.BookingId;
        sheet.Cell(row, 2).Value = item.CustomerName;
        sheet.Cell(row, 3).Value = item.Phone;
        sheet.Cell(row, 4).Value = item.ServiceName;
        sheet.Cell(row, 5).Value = item.ScheduledTime.ToString("yyyy-MM-dd HH:mm");
        sheet.Cell(row, 6).Value = item.StatusLabel;
      }

      sheet.Columns().AdjustToContents();

      using var stream = new MemoryStream();
      workbook.SaveAs(stream);
      return stream.ToArray();
    }

    private async Task<List<Booking>> FilteredBookingsAsync(DateTime start, DateTime end, int? serviceId)
    {
      var query = _context.Bookings.Where(b => b.CreatedAt >= start && b.CreatedAt < end);
      if (serviceId.HasValue)
      {
        query = query.Where(b => b.ServiceID == serviceId.Value);
      }
      return await query.ToListAsync();
    }

    private async Task<List<UnfinishedBookingItemDto>> MapUnfinishedBookingsAsync(List<Booking> bookings)
    {
      var serviceNames = await _context.Services.ToDictionaryAsync(s => s.ServiceID, s => s.ServiceName);
      var customerNames = await _context.Customers.ToDictionaryAsync(c => c.CustomerID, c => c.FullName);

      return bookings.Select(b => new UnfinishedBookingItemDto
      {
        BookingId = b.BookingID,
        CustomerName = customerNames.TryGetValue(b.CustomerID, out var name) && !string.IsNullOrWhiteSpace(name)
            ? name
            : (string.IsNullOrWhiteSpace(b.Phone) ? "Khách vãng lai" : b.Phone),
        Phone = b.Phone,
        ServiceName = serviceNames.TryGetValue(b.ServiceID, out var svcName) ? svcName : "Dịch vụ không xác định",
        ScheduledTime = b.ScheduledTime,
        Status = b.Status.ToString(),
        StatusLabel = StatusLabel(b.Status, b.CheckInTime.HasValue)
      }).ToList();
    }

    private static string StatusLabel(BookingStatus status, bool checkedIn) => status switch
    {
      BookingStatus.Completed => "Hoàn thành",
      BookingStatus.Cancelled => "Hủy",
      BookingStatus.NoShow => "No-show",
      BookingStatus.Failed => "Thất bại",
      BookingStatus.Pending => checkedIn ? "Đang xử lý" : "Đang chờ",
      _ => status.ToString()
    };

    private static List<StatusBreakdownItemDto> BuildStatusBreakdown(CompletionOverviewDto overview)
    {
      var total = overview.TotalBookings;
      var items = new (string Status, string Label, int Count)[]
      {
        ("Completed", "Hoàn thành", overview.CompletedBookings),
        ("Cancelled", "Hủy", overview.CancelledBookings),
        ("NoShow", "No-show", overview.NoShowBookings),
        ("Pending", "Đang chờ", overview.PendingBookings),
        ("Processing", "Đang xử lý", overview.ProcessingBookings),
        ("Failed", "Thất bại", overview.FailedBookings)
      };

      return items
          .Select(x => new StatusBreakdownItemDto
          {
            Status = x.Status,
            Label = x.Label,
            Count = x.Count,
            Percentage = total == 0 ? 0m : Math.Round((decimal)x.Count * 100m / total, 2)
          })
          .ToList();
    }

    private static List<FailureReasonItemDto> BuildFailureReasons(int cancelledBookings, int noShowBookings, int failedBookings)
    {
      var unfinished = cancelledBookings + noShowBookings + failedBookings;
      var items = new (string Status, string Label, int Count)[]
      {
        ("Cancelled", "Khách/Admin hủy", cancelledBookings),
        ("NoShow", "No-show", noShowBookings),
        ("Failed", "Sự cố / Thất bại", failedBookings)
      };

      return items
          .Select(x => new FailureReasonItemDto
          {
            Status = x.Status,
            Label = x.Label,
            Count = x.Count,
            Percentage = unfinished == 0 ? 0m : Math.Round((decimal)x.Count * 100m / unfinished, 2)
          })
          .OrderByDescending(x => x.Count)
          .ToList();
    }

    private static List<ServiceCompletionItemDto> BuildTopServices(List<Booking> bookings, List<Service> services)
    {
      return bookings
          .GroupBy(b => b.ServiceID)
          .Select(g =>
          {
            var service = services.FirstOrDefault(s => s.ServiceID == g.Key);
            var serviceName = service?.ServiceName ?? "Dịch vụ không xác định";
            if (service?.Status == "Deleted")
            {
              serviceName = $"{serviceName} (Đã xóa)";
            }

            var total = g.Count();
            var completed = g.Count(b => b.Status == BookingStatus.Completed);

            return new ServiceCompletionItemDto
            {
              ServiceId = g.Key,
              ServiceName = serviceName,
              TotalBookings = total,
              CompletedBookings = completed,
              CompletionRate = total == 0 ? 0m : Math.Round((decimal)completed * 100m / total, 2)
            };
          })
          .OrderByDescending(x => x.TotalBookings)
          .ToList();
    }

    // Buckets are built dynamically in 2-hour windows from the ScheduledTime data actually present,
    // rather than assuming an unverified fixed operating-hours window.
    private static List<TimeSlotCompletionItemDto> BuildTimeSlots(List<Booking> bookings)
    {
      if (bookings.Count == 0)
      {
        return new List<TimeSlotCompletionItemDto>();
      }

      var withHour = bookings
          .Select(b => new { Booking = b, Hour = b.ScheduledTime.Add(VietnamOffset).Hour })
          .ToList();

      var minBucket = withHour.Min(x => x.Hour) / 2 * 2;
      var maxBucket = withHour.Max(x => x.Hour) / 2 * 2;

      var slots = new List<TimeSlotCompletionItemDto>();
      for (var bucketStart = minBucket; bucketStart <= maxBucket; bucketStart += 2)
      {
        var bucketEnd = bucketStart + 2;
        var inSlot = withHour.Where(x => x.Hour >= bucketStart && x.Hour < bucketEnd).ToList();
        if (inSlot.Count == 0)
        {
          continue;
        }

        var completed = inSlot.Count(x => x.Booking.Status == BookingStatus.Completed);
        slots.Add(new TimeSlotCompletionItemDto
        {
          TimeSlot = $"{bucketStart:D2}:00-{bucketEnd:D2}:00",
          TotalBookings = inSlot.Count,
          CompletedBookings = completed,
          CompletionRate = Math.Round((decimal)completed * 100m / inSlot.Count, 2)
        });
      }

      return slots;
    }

    private static List<TrendPointDto> BuildTrend(List<Booking> bookings, string? groupBy)
    {
      var mode = string.IsNullOrWhiteSpace(groupBy) ? "day" : groupBy.ToLower();

      DateTime BucketStart(DateTime createdAtUtc)
      {
        var vnDate = createdAtUtc.Add(VietnamOffset).Date;
        return mode switch
        {
          "month" => new DateTime(vnDate.Year, vnDate.Month, 1),
          "week" => vnDate.AddDays(-1 * ((7 + (vnDate.DayOfWeek - DayOfWeek.Monday)) % 7)),
          _ => vnDate
        };
      }

      string BucketLabel(DateTime bucketStart) => mode switch
      {
        "month" => bucketStart.ToString("yyyy-MM"),
        "week" => $"Tuần {System.Globalization.ISOWeek.GetWeekOfYear(bucketStart)}/{bucketStart.Year}",
        _ => bucketStart.ToString("yyyy-MM-dd")
      };

      return bookings
          .GroupBy(b => BucketStart(b.CreatedAt))
          .OrderBy(g => g.Key)
          .Select(g =>
          {
            var total = g.Count();
            var completed = g.Count(b => b.Status == BookingStatus.Completed);
            var cancelled = g.Count(b => b.Status == BookingStatus.Cancelled);
            var noShow = g.Count(b => b.Status == BookingStatus.NoShow);

            return new TrendPointDto
            {
              PeriodLabel = BucketLabel(g.Key),
              PeriodStart = g.Key,
              TotalBookings = total,
              CompletedBookings = completed,
              CancelledBookings = cancelled,
              NoShowBookings = noShow,
              CompletionRate = total == 0 ? 0m : Math.Round((decimal)completed * 100m / total, 2),
              CancellationRate = total == 0 ? 0m : Math.Round((decimal)cancelled * 100m / total, 2),
              NoShowRate = total == 0 ? 0m : Math.Round((decimal)noShow * 100m / total, 2)
            };
          })
          .ToList();
    }

    private static List<AlertDto> BuildAlerts(CompletionOverviewDto overview, int cancelledBookings, int previousCancelledBookings, List<ServiceCompletionItemDto> topServices)
    {
      var alerts = new List<AlertDto>();
      if (overview.TotalBookings == 0)
      {
        return alerts;
      }

      var completionPercentage = Math.Round(overview.CompletionRate * 100, 2);
      if (completionPercentage < 80m)
      {
        alerts.Add(new AlertDto
        {
          Severity = "Critical",
          Code = "LOW_COMPLETION_RATE",
          Message = $"Tỷ lệ hoàn thành ({completionPercentage:0.##}%) đang dưới ngưỡng 80%."
        });
      }

      var noShowPercentage = Math.Round((decimal)overview.NoShowBookings * 100m / overview.TotalBookings, 2);
      if (noShowPercentage > 10m)
      {
        alerts.Add(new AlertDto
        {
          Severity = "Warning",
          Code = "HIGH_NOSHOW_RATE",
          Message = $"Tỷ lệ No-show ({noShowPercentage:0.##}%) đang trên ngưỡng 10%."
        });
      }

      if (previousCancelledBookings > 0)
      {
        var increase = (decimal)(cancelledBookings - previousCancelledBookings) * 100m / previousCancelledBookings;
        if (increase > 20m)
        {
          alerts.Add(new AlertDto
          {
            Severity = "Warning",
            Code = "CANCELLATION_SPIKE",
            Message = $"Số booking bị hủy tăng {increase:0.##}% so với kỳ trước."
          });
        }
      }

      foreach (var service in topServices.Where(s => s.TotalBookings >= 5))
      {
        var notCompletedRate = 100m - service.CompletionRate;
        if (notCompletedRate > 20m)
        {
          alerts.Add(new AlertDto
          {
            Severity = "Warning",
            Code = "SERVICE_HIGH_CANCEL_RATE",
            Message = $"Dịch vụ '{service.ServiceName}' có tỷ lệ không hoàn thành cao ({notCompletedRate:0.##}%)."
          });
        }
      }

      return alerts;
    }

    private static KpiDto BuildKpi(decimal completionPercentage)
    {
      if (completionPercentage >= 95m)
      {
        return new KpiDto { Level = "Excellent", Label = "Xuất sắc", Color = "Green" };
      }
      if (completionPercentage >= 90m)
      {
        return new KpiDto { Level = "Good", Label = "Tốt", Color = "Green" };
      }
      if (completionPercentage >= 80m)
      {
        return new KpiDto { Level = "Average", Label = "Trung bình", Color = "Yellow" };
      }
      return new KpiDto { Level = "NeedsImprovement", Label = "Cần cải thiện", Color = "Red" };
    }

    private static List<string> BuildInsights(CompletionOverviewDto overview, PeriodComparisonDto comparison, List<ServiceCompletionItemDto> topServices, List<TimeSlotCompletionItemDto> timeSlots)
    {
      var insights = new List<string>();
      if (overview.TotalBookings == 0)
      {
        return insights;
      }

      var completionPercentage = Math.Round(overview.CompletionRate * 100, 2);
      if (comparison.DeltaPercentagePoints > 0)
      {
        insights.Add($"Tỷ lệ hoàn thành đạt {completionPercentage:0.##}%, tăng {comparison.DeltaPercentagePoints:0.##} điểm % so với kỳ trước.");
      }
      else if (comparison.DeltaPercentagePoints < 0)
      {
        insights.Add($"Tỷ lệ hoàn thành đạt {completionPercentage:0.##}%, giảm {Math.Abs(comparison.DeltaPercentagePoints):0.##} điểm % so với kỳ trước.");
      }
      else
      {
        insights.Add($"Tỷ lệ hoàn thành đạt {completionPercentage:0.##}%, không đổi so với kỳ trước.");
      }

      var noShowPercentage = Math.Round((decimal)overview.NoShowBookings * 100m / overview.TotalBookings, 2);
      insights.Add($"Tỷ lệ No-show hiện ở mức {noShowPercentage:0.##}%.");

      var bestService = topServices.Where(s => s.TotalBookings >= 5).OrderByDescending(s => s.CompletionRate).FirstOrDefault();
      if (bestService != null)
      {
        insights.Add($"Dịch vụ '{bestService.ServiceName}' có tỷ lệ hoàn thành cao nhất ({bestService.CompletionRate:0.##}%).");
      }

      var worstSlot = timeSlots.Where(s => s.TotalBookings >= 5).OrderBy(s => s.CompletionRate).FirstOrDefault();
      if (worstSlot != null && worstSlot.CompletionRate < 90m)
      {
        insights.Add($"Khung giờ {worstSlot.TimeSlot} có tỷ lệ hoàn thành thấp nhất ({worstSlot.CompletionRate:0.##}%), nên cân nhắc tăng nhân sự hoặc xác nhận lịch trước với khách.");
      }

      return insights;
    }

  }
}
