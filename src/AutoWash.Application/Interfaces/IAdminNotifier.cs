using System.Threading.Tasks;

namespace AutoWash.Application.Interfaces
{
    // Abstraction cho việc đẩy thông báo real-time tới các Admin đang online.
    // Application layer chỉ biết tới interface này — không phụ thuộc SignalR/ASP.NET Core,
    // giống cách IApplicationDbContext tách Application khỏi EF Core cụ thể (Dependency Inversion).
    // Implementation thật (dùng SignalR IHubContext) nằm ở AutoWashPro.API/Hubs/SignalRAdminNotifier.cs.
    public interface IAdminNotifier
    {
        Task NotifyNewBookingAsync(int bookingId, string customerName, string licensePlate, string serviceName);

        Task NotifyBookingStatusChangedAsync(int bookingId, string customerName, string licensePlate, string oldStatus, string newStatus);

        Task NotifyNewCustomerAsync(int customerId, string fullName, string phone);
    }
}
