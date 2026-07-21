using System.Threading.Tasks;

namespace AutoWash.Application.Interfaces
{
    // Abstraction cho việc đẩy sự kiện real-time về tình trạng khung giờ tới TẤT CẢ
    // client đang xem trang đặt lịch (không chỉ Admin — khác với IAdminNotifier).
    // Implementation thật (SignalR IHubContext<BookingHub>) nằm ở AutoWashPro.API/Hubs/SignalRBookingHubNotifier.cs.
    public interface IBookingHubNotifier
    {
        Task NotifySlotOccupancyChangedAsync(string date, string slotTime, int availableCount, string status);
    }
}
