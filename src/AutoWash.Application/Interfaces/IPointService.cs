using System.Collections.Generic;
using System.Threading.Tasks;
using AutoWash.Application.DTOs;

namespace AutoWash.Application.Interfaces
{
    public interface IPointService
    {
        // BR-53 (ISSUE-09): tính điểm từ bookingId, MIN(FLOOR(FinalAmount/10000), 500)
        Task<int> EarnPointsAsync(int bookingId);

        // GET /api/loyalty — xem ví điểm + batch hết hạn (BR-59)
        Task<LoyaltyWalletResponse> GetWalletAsync(int customerId);

        // GET /api/loyalty/history — lịch sử PointTransaction
        Task<IEnumerable<PointHistoryResponse>> GetHistoryAsync(int customerId);

        // GET /api/loyalty/simulate — preview đổi thưởng (BR-60)
        Task<RedeemSimulateResponse> SimulateRedemptionAsync(int customerId, int rewardId, decimal baseAmount);

        // BR-57: gọi bởi PointExpiryJob hàng ngày
        Task RunDailyExpiryAsync();
    }
}
