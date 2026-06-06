using Domain.Entities;
using Infrastructure.Persistence;
using Application.Interfaces;

namespace Infrastructure.Repositories
{
    public class ServiceRepository : IServiceRepository
    {
        private readonly CarWashDbContext _context;

        public ServiceRepository(CarWashDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Service>> GetAllAsync()
        {
            return _context.Services.ToList();
        }

        public async Task<Service?> GetByIdAsync(int id)
        {
            return await _context.Services.FindAsync(id);
        }

        public async Task<Service> AddAsync(Service entity)
        {
            await _context.Services.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(Service entity)
        {
            _context.Services.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.Services.FindAsync(id);
            if (entity != null)
            {
                _context.Services.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }

    public class RewardRepository : IRewardRepository
    {
        private readonly CarWashDbContext _context;

        public RewardRepository(CarWashDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<RewardsCatalog>> GetAllAsync()
        {
            return _context.RewardsCatalogs.ToList();
        }

        public async Task<RewardsCatalog?> GetByIdAsync(int id)
        {
            return await _context.RewardsCatalogs.FindAsync(id);
        }

        public async Task<RewardsCatalog> AddAsync(RewardsCatalog entity)
        {
            await _context.RewardsCatalogs.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(RewardsCatalog entity)
        {
            _context.RewardsCatalogs.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.RewardsCatalogs.FindAsync(id);
            if (entity != null)
            {
                _context.RewardsCatalogs.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }
}
