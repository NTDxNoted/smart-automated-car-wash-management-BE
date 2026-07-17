using System.Threading.Tasks;
using AutoWash.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace AutoWashPro.API.Hubs
{
    // Implementation thật của IAdminNotifier — bọc IHubContext<AdminNotificationHub> để đẩy
    // sự kiện tới TẤT CẢ client đang connect vào hub (chỉ Admin connect được vì Hub có [Authorize(Roles="ADMIN")]).
    // Tên event ("NewBooking", "BookingStatusChanged", "NewCustomer") phải khớp với tên
    // FE lắng nghe qua connection.on(...) trong signalrService.js.
    public class SignalRAdminNotifier : IAdminNotifier
    {
        private readonly IHubContext<AdminNotificationHub> _hubContext;

        public SignalRAdminNotifier(IHubContext<AdminNotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task NotifyNewBookingAsync(int bookingId, string customerName, string licensePlate, string serviceName)
        {
            return _hubContext.Clients.All.SendAsync("NewBooking", new
            {
                bookingId,
                customerName,
                licensePlate,
                serviceName
            });
        }

        public Task NotifyBookingStatusChangedAsync(int bookingId, string customerName, string licensePlate, string oldStatus, string newStatus)
        {
            return _hubContext.Clients.All.SendAsync("BookingStatusChanged", new
            {
                bookingId,
                customerName,
                licensePlate,
                oldStatus,
                newStatus
            });
        }

        public Task NotifyNewCustomerAsync(int customerId, string fullName, string phone)
        {
            return _hubContext.Clients.All.SendAsync("NewCustomer", new
            {
                customerId,
                fullName,
                phone
            });
        }
    }
}
