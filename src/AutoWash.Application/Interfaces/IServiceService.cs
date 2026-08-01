using AutoWash.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoWash.Application.Interfaces
{
    public interface IServiceService
    {
        Task<IEnumerable<ServiceResponse>> GetActiveServicesAsync();
        Task<IEnumerable<ServiceResponse>> GetAllServicesForAdminAsync();
        Task<ServiceResponse> GetServiceByIdAsync(int serviceId);
        Task<ServiceResponse> CreateServiceAsync(CreateServiceRequest request);
        Task<ServiceResponse> UpdateServiceAsync(int serviceId, UpdateServiceRequest request);
        Task<ServiceResponse> ToggleServiceStatusAsync(int serviceId);
        Task<ServiceResponse> DeleteServiceAsync(int serviceId);
    }
}
