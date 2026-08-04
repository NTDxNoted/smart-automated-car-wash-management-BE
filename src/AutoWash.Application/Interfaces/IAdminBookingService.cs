using System.Collections.Generic;
using System.Threading.Tasks;
using AutoWash.Application.DTOs.Admin;

namespace AutoWash.Application.Interfaces
{
    public interface IAdminBookingService
    {
        Task<IEnumerable<AdminBookingListResponse>> GetAllBookingsAsync(string? status, string? date, string? phone, string? plate);
        Task<AdminBookingListResponse> GetBookingByIdAsync(int id);
        Task<object> UpdateBookingStatusAsync(int id, UpdateBookingStatusRequest request);
        Task<object> UpdateLicensePlateAsync(int id, UpdateLicensePlateRequest request);
        Task<object> CheckInAsync(int id);
        Task<object> EmergencyStopAsync(int id, EmergencyStopRequest request);
        Task<AdminBookingListResponse> CreateWalkInBookingAsync(CreateWalkInBookingRequest request);
    }
}
