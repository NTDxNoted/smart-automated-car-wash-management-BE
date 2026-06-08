using System.Collections.Generic;
using System.Threading.Tasks;
using AutoWash.Application.DTOs;

namespace AutoWash.Application.Interfaces
{
    public interface ITierService
    {
        Task<IEnumerable<TierResponse>> GetAllTiersAsync();
        Task<TierResponse> UpdateTierAsync(int tierId, UpdateTierRequest request);

        // BR-21: gọi sau mỗi lần Booking → Completed, upgrade real-time
        Task EvaluateUpgradeAsync(int customerId);

        // BR-22: gọi bởi TierDowngradeJob vào mùng 1 hàng tháng
        Task RunMonthlyDowngradeAsync();
    }
}
