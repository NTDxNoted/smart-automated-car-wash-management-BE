// Application/Services/ServiceService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services
{
    public class ServiceService : IServiceService
    {
        private readonly IServiceRepository _repository;

        public ServiceService(IServiceRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<ServiceResponse>> GetActiveServicesAsync()
        {
            var services = await _repository.GetAllAsync();
            return services.Where(s => s.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                           .Select(MapToResponse);
        }

        public async Task<ServiceResponse?> GetServiceByIdAsync(int id)
        {
            var service = await _repository.GetByIdAsync(id);
            return service == null ? null : MapToResponse(service);
        }

        public async Task<ServiceResponse> CreateServiceAsync(CreateServiceRequest request)
        {
            var service = new Service
            {
                ServiceName = request.ServiceName,
                ServiceCategory = request.ServiceCategory,
                Description = request.Description,
                Price = request.Price,
                Duration = request.Duration,
                Status = "Active"
            };

            var created = await _repository.AddAsync(service);
            return MapToResponse(created);
        }

        public async Task<bool> UpdateServiceAsync(int id, UpdateServiceRequest request)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return false;

            existing.ServiceName = request.ServiceName;
            existing.ServiceCategory = request.ServiceCategory;
            existing.Description = request.Description;
            existing.Price = request.Price;
            existing.Duration = request.Duration;

            await _repository.UpdateAsync(existing);
            return true;
        }

        public async Task<bool> ToggleServiceStatusAsync(int id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return false;

            existing.Status = existing.Status.Equals("Active", StringComparison.OrdinalIgnoreCase)
                ? "Inactive"
                : "Active";

            await _repository.UpdateAsync(existing);
            return true;
        }

        private ServiceResponse MapToResponse(Service service) => new()
        {
            ServiceId = service.ServiceId,
            ServiceName = service.ServiceName,
            ServiceCategory = service.ServiceCategory,
            Description = service.Description,
            Price = service.Price,
            Duration = service.Duration,
            Status = service.Status
        };
    }
}