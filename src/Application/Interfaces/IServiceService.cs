// IServiceService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;

namespace Application.Interfaces
{
    public interface IServiceService
    {
        Task<IEnumerable<ServiceResponse>> GetActiveServicesAsync();
        Task<ServiceResponse?> GetServiceByIdAsync(int id);
        Task<ServiceResponse> CreateServiceAsync(CreateServiceRequest request);
        Task<bool> UpdateServiceAsync(int id, UpdateServiceRequest request);
        Task<bool> ToggleServiceStatusAsync(int id);
    }
}