using System.Collections.Generic;
using System.Threading.Tasks;
using AutoWash.Application.DTOs;

namespace AutoWash.Application.Interfaces
{
    public interface IBookingsService
    {
        // POST /api/Bookings
        Task<BookingResponseDto> CreateBookingAsync(CreateBookingRequest request, int? customerId);

        // GET /api/Bookings
        Task<PagedResponse<BookingResponseDto>> GetCustomerBookingsAsync(
            int? customerId,
            string? guestPhone,
            string? status,
            int page,
            int pageSize);

        // GET /api/Bookings/{id}
        Task<BookingResponseDto> GetBookingByIdAsync(
            int bookingId,
            int? customerId,
            string? guestPhone);

        // POST /api/Bookings/{id}/cancel
        Task<CancelBookingResponseDto> CancelBookingAsync(
            int bookingId,
            int? customerId,
            string? guestPhone);

        // POST /api/Bookings/{id}/complete
        Task<BookingResponseDto> CompleteBookingAsync(int bookingId);

        // Lấy danh sách time slots khả dụng
        Task<IEnumerable<AvailableSlotResponse>> GetAvailableSlotsAsync(int? customerId, string? dateStr, string? licensePlate);
    }
}