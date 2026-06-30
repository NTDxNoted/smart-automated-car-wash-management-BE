using System.Collections.Generic;
using System.Threading.Tasks;
using AutoWash.Application.DTOs;

namespace AutoWash.Application.Interfaces
{
    public interface IBookingsService
    {
        Task<BookingResponseDto> CreateBookingAsync(CreateBookingRequest request, int? customerId);
        Task<PagedResponse<BookingResponseDto>> GetCustomerBookingsAsync(
            int? customerId,
            string? guestPhone,
            string? status,
            int page,
            int pageSize);
        Task<BookingResponseDto> GetBookingByIdAsync(
            int bookingId,
            int? customerId,
            string? guestPhone);
        Task<CancelBookingResponseDto> CancelBookingAsync(
            int bookingId,
            int? customerId,
            string? guestPhone);
        Task<BookingResponseDto> CompleteBookingAsync(int bookingId);
        Task<IEnumerable<AvailableSlotResponse>> GetAvailableSlotsAsync(int? customerId, string? dateStr, string? licensePlate);
    }
}