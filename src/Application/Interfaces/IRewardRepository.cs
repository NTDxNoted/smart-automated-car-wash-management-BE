using Domain.Entities;

namespace Application.Interfaces
{
    public interface IRewardRepository
    {
        Task<IEnumerable<RewardsCatalog>> GetAllAsync();
        Task<RewardsCatalog?> GetByIdAsync(int id);
        Task<RewardsCatalog> AddAsync(RewardsCatalog entity);
        Task UpdateAsync(RewardsCatalog entity);
        Task DeleteAsync(int id);
    }
}
