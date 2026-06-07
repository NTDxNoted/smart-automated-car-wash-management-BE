using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Entities;

namespace AutoWash.Application.Services
{
    public class ServiceService : IServiceService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<ServiceService> _logger;

        public ServiceService(IApplicationDbContext context, ILogger<ServiceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<ServiceResponse>> GetActiveServicesAsync()
        {
            return await _context.Services
                .Where(s => s.Status == "Active")
                .Select(s => new ServiceResponse
                {
                    ServiceId = s.ServiceID,
                    ServiceName = s.ServiceName,
                    ServiceCategory = s.ServiceCategory,
                    Description = s.Description,
                    Price = s.Price,
                    Duration = s.Duration,
                    Status = s.Status
                })
                .ToListAsync();
        }

        public async Task<ServiceResponse> GetServiceByIdAsync(int serviceId)
        {
            var service = await _context.Services.FirstOrDefaultAsync(s => s.ServiceID == serviceId);
            if (service == null)
                throw new Exception("NOT_FOUND: Không tìm thấy dịch vụ.");
            return MapToResponse(service);
        }

        public async Task<ServiceResponse> CreateServiceAsync(CreateServiceRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ServiceName) ||
                string.IsNullOrWhiteSpace(request.ServiceCategory) ||
                string.IsNullOrWhiteSpace(request.Description) ||
                request.Price <= 0 || request.Duration <= 0)
                throw new Exception("INVALID_REQUEST: Thiếu thông tin bắt buộc (BR-35).");

            var service = new Service
            {
                ServiceName = request.ServiceName,
                ServiceCategory = request.ServiceCategory,
                Description = request.Description,
                Price = request.Price,
                Duration = request.Duration,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            _context.Services.Add(service);
            await _context.SaveChangesAsync();
            return MapToResponse(service);
        }

        public async Task<ServiceResponse> UpdateServiceAsync(int serviceId, UpdateServiceRequest request)
        {
            var service = await _context.Services.FirstOrDefaultAsync(s => s.ServiceID == serviceId);
            if (service == null)
                throw new Exception("NOT_FOUND: Không tìm thấy dịch vụ.");

            if (request.ServiceName != null) service.ServiceName = request.ServiceName;
            if (request.ServiceCategory != null) service.ServiceCategory = request.ServiceCategory;
            if (request.Description != null) service.Description = request.Description;
            if (request.Price.HasValue) service.Price = request.Price.Value;
            if (request.Duration.HasValue) service.Duration = request.Duration.Value;

            await _context.SaveChangesAsync();
            return MapToResponse(service);
        }

        public async Task<ServiceResponse> ToggleServiceStatusAsync(int serviceId)
        {
            var service = await _context.Services.FirstOrDefaultAsync(s => s.ServiceID == serviceId);
            if (service == null)
                throw new Exception("NOT_FOUND: Không tìm thấy dịch vụ.");

            service.Status = service.Status == "Active" ? "Inactive" : "Active";
            await _context.SaveChangesAsync();
            return MapToResponse(service);
        }

        private static ServiceResponse MapToResponse(Service s) => new ServiceResponse
        {
            ServiceId = s.ServiceID,
            ServiceName = s.ServiceName,
            ServiceCategory = s.ServiceCategory,
            Description = s.Description,
            Price = s.Price,
            Duration = s.Duration,
            Status = s.Status
        };
    }
}
