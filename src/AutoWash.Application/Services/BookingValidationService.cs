using System;
using System.Linq;
using System.Threading.Tasks;
using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Entities;
using AutoWash.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AutoWash.Application.Services
{
  public class BookingValidationService : IBookingValidationService
  {
    private readonly IApplicationDbContext _context;

    public BookingValidationService(IApplicationDbContext context)
    {
      _context = context;
    }

    public async Task ValidateCreateBookingAsync(CreateBookingRequest request, Customer? customer, Service service, string licensePlate)
    {
      var now = DateTime.UtcNow;
      if (request.ScheduledTime <= now.AddMinutes(60))
        throw new InvalidOperationException("ADVANCE_NOTICE_VIOLATION: Cần đặt lịch trước ít nhất 60 phút.");

      if (customer != null && customer.SuspendedUntil.HasValue && customer.SuspendedUntil.Value > now)
        throw new InvalidOperationException("CUSTOMER_SUSPENDED: Tài khoản của bạn đang bị tạm khóa.");

      if (customer == null)
      {
        var guestPendingCount = await _context.Bookings
            .CountAsync(b => b.Phone == request.Phone && b.Status == BookingStatus.Pending);
        if (guestPendingCount >= 1)
          throw new InvalidOperationException("PENDING_QUOTA_EXCEEDED: Bạn chỉ được có 1 lịch hẹn đang chờ xác nhận.");
      }
      else
      {
        var memberPendingCount = await _context.Bookings
            .CountAsync(b => b.CustomerID == customer.CustomerID && b.Status == BookingStatus.Pending);
        if (memberPendingCount >= 3)
          throw new InvalidOperationException("PENDING_QUOTA_EXCEEDED: Bạn đã có 3 lịch hẹn đang chờ xác nhận.");
      }

      var dateStart = request.ScheduledTime.Date;
      var dateEnd = dateStart.AddDays(1);
      var unfinishedTodayCount = await _context.Bookings
          .CountAsync(b => b.LicensePlate == licensePlate && b.ScheduledTime >= dateStart && b.ScheduledTime < dateEnd && b.Status != BookingStatus.Completed);
      if (unfinishedTodayCount >= 2)
        throw new InvalidOperationException("DAILY_INCOMPLETE_LIMIT_EXCEEDED: Bạn chỉ được có tối đa 2 lịch chưa hoàn thành trong ngày.");

      var bufferStart = request.ScheduledTime.AddMinutes(-120);
      var bufferEnd = request.ScheduledTime.AddMinutes(120);
      var existingWarn = await _context.Bookings
          .Where(b => b.LicensePlate == licensePlate)
          .Where(b => b.ScheduledTime >= bufferStart && b.ScheduledTime <= bufferEnd)
          .AnyAsync();
      if (existingWarn)
        throw new InvalidOperationException("VEHICLE_BUFFER_VIOLATION: Biển số này đã có lịch hẹn trong vòng 120 phút.");

      if (customer != null)
      {
        var bookingWindowLimit = GetBookingWindowLimit(customer.TierID);
        if (request.ScheduledTime > now.AddDays(bookingWindowLimit))
          throw new InvalidOperationException("BOOKING_WINDOW_VIOLATION: Khung giờ đặt lịch vượt quá giới hạn hạng thành viên.");
      }

      var slotStart = request.ScheduledTime.AddMinutes(-5);
      var slotEnd = request.ScheduledTime.AddMinutes(5);
      var slotConflict = await _context.Bookings
          .Where(b => b.ServiceID == service.ServiceID)
          .Where(b => b.ScheduledTime >= slotStart && b.ScheduledTime <= slotEnd)
          .AnyAsync();
      if (slotConflict)
        throw new InvalidOperationException("SLOT_NOT_AVAILABLE: Khung giờ này đã có người đặt.");
    }

    private static int GetBookingWindowLimit(int tierId)
    {
      return tierId switch
      {
        1 => 14,
        2 => 30,
        _ => 7,
      };
    }
  }
}
