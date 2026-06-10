using System.Threading.Tasks;

namespace AutoWash.Application.Interfaces
{
    public interface IPointService
    {
        Task<int> EarnPointsAsync(int bookingId);
    }
}
