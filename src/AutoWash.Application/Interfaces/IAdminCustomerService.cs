using System.Threading.Tasks;
using AutoWash.Application.DTOs;

namespace AutoWash.Application.Interfaces
{
    public interface IAdminCustomerService
    {
        // 1. Lấy danh sách (Phân trang, filter theo tier, trạng thái lock)
        Task<PagedResponse<CustomerAdminResponseDto>> GetCustomersAsync(string? tier, bool? isLocked, int page, int pageSize);

        // 2. Lấy chi tiết hồ sơ
        Task<CustomerDetailAdminResponseDto> GetCustomerByIdAsync(int customerId);

        // 3. Khóa/Mở khóa tài khoản (Toggle Lock)
        Task<LockCustomerResponseDto> ToggleLockCustomerAsync(int customerId);

        // 4. Cập nhật ghi chú Admin
        Task<CustomerAdminResponseDto> UpdateCustomerNotesAsync(int customerId, string? notes);
    }
}