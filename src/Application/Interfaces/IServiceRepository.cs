using Domain.Entities;

namespace Application.Interfaces
{
    public interface IServiceRepository
    {
        Task<IEnumerable<Service>> GetAllAsync();
        Task<Service?> GetByIdAsync(int id);
        Task<Service> AddAsync(Service entity);
        Task UpdateAsync(Service entity);
        Task DeleteAsync(int id);
    }
}
