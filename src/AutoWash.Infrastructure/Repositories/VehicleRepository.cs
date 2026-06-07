using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoWash.Domain.Entities;
using AutoWash.Infrastructure.Data;

namespace AutoWash.Infrastructure.Repositories
{
    public class VehicleRepository
    {
        private readonly ApplicationDbContext _context;

        public VehicleRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Vehicle>> GetByCustomerIdAsync(int customerId)
        {
            return await _context.Vehicles
                .Where(v => v.CustomerID == customerId && v.IsActive)
                .ToListAsync();
        }
    }
}
