using System.Threading.Tasks;
using AutoWash.Application.DTOs;

namespace AutoWash.Application.Interfaces
{
    public interface ICustomerService
    {
        Task<ProfileResponse> GetProfileAsync(int customerId);
        Task<ProfileResponse> UpdateProfileAsync(int customerId, UpdateProfileRequest request);
    }
}
