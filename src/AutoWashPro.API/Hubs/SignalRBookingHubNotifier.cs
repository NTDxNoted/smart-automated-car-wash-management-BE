using System.Threading.Tasks;
using AutoWash.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace AutoWashPro.API.Hubs
{
    // Implementation thật của IBookingHubNotifier — bọc IHubContext<BookingHub> để đẩy
    // sự kiện tới TẤT CẢ client đang connect vào hub (công khai, không cần đăng nhập).
    // Tên event ("SlotOccupancyUpdated") phải khớp với tên FE lắng nghe qua connection.on(...).
    public class SignalRBookingHubNotifier : IBookingHubNotifier
    {
        private readonly IHubContext<BookingHub> _hubContext;

        public SignalRBookingHubNotifier(IHubContext<BookingHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task NotifySlotOccupancyChangedAsync(string date, string slotTime, int availableCount, string status)
        {
            return _hubContext.Clients.All.SendAsync("SlotOccupancyUpdated", new
            {
                date,
                slotTime,
                availableCount,
                status
            });
        }
    }
}
