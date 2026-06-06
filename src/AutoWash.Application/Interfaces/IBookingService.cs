using System.Threading.Tasks;
using AutoWash.Application.DTOs;

namespace AutoWash.Application.Interfaces
{
    public interface IBookingsService // Đổi tên thành IBookingsService (số nhiều) cho đúng convention
    {
        // Cập nhật: cho phép customerId là null (dành cho Guest)
        Task<PagedResponse<BookingResponseDto>> GetCustomerBookingsAsync(int? customerId, string? guestPhone, string? status, int page, int pageSize);

        // Cập nhật: cho phép customerId là null
        Task<BookingResponseDto> GetBookingByIdAsync(int bookingId, int? customerId, string? guestPhone);

        // Hàm Cancel của bạn đã chuẩn rồi
        Task<CancelBookingResponseDto> CancelBookingAsync(int bookingId, int? customerId, string? guestPhone);
        Task GetCustomerBookingsAsync(int customerId, string? status, int page, int pageSize);
    }
}