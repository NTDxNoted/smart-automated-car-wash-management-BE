using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Entities;

namespace AutoWash.Infrastructure.Repositories
{
    public class ServiceRepository
    {
        private readonly IApplicationDbContext _context;

        public ServiceRepository(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Service>> GetActiveServicesAsync()
        {
            return await _context.Services
                .Where(s => s.Status == "Active")
                .ToListAsync();
        }

        public async Task<Service?> GetByIdAsync(int serviceId)
        {
            return await _context.Services.FirstOrDefaultAsync(s => s.ServiceID == serviceId);
        }
    }
}
