using System.Threading.Tasks;
using AutoWash.Application.DTOs;

namespace AutoWash.Application.Interfaces
{
    public interface IAdminAuthService
    {
        Task<AdminLoginResponse> LoginAsync(AdminLoginRequest request);
        Task<AdminProfileResponse> GetProfileAsync(int adminId);
    }
}
