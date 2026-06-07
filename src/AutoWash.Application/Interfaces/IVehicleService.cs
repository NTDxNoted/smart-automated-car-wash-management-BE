using System.Collections.Generic;
using System.Threading.Tasks;
using AutoWash.Application.DTOs;

namespace AutoWash.Application.Interfaces
{
    public interface IVehicleService
    {
        Task<IEnumerable<VehicleResponse>> GetVehiclesAsync(int customerId);
        Task<VehicleResponse> AddVehicleAsync(int customerId, AddVehicleRequest request);
        Task<VehicleResponse> UpdateVehicleAsync(int customerId, int vehicleId, UpdateVehicleRequest request);
        Task DeleteVehicleAsync(int customerId, int vehicleId);
    }
}
