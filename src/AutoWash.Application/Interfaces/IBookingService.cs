using System.Threading.Tasks;
using AutoWash.Application.DTOs;

namespace AutoWash.Application.Interfaces
{
    public interface IBookingService
    {
        // 1. GET /api/bookings (Có phân trang và filter)
        Task<PagedResponse<BookingResponseDto>> GetCustomerBookingsAsync(int customerId, string? status, int page, int pageSize);

        // 2. GET /api/bookings/{id}
        Task<BookingResponseDto> GetBookingByIdAsync(int bookingId, int customerId);

        // 3. POST /api/bookings/{id}/cancel (Chứa logic hủy 2 tiếng và hoàn điểm)
        Task<CancelBookingResponseDto> CancelBookingAsync(int bookingId, int customerId);
    }
}