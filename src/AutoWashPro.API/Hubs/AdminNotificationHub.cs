using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AutoWashPro.API.Hubs
{
    // SignalR Hub — thay cho polling mỗi 5 giây ở FE (AdminLayout.jsx cũ).
    // Chỉ Admin đã đăng nhập mới connect được (dùng chung JWT Bearer đã cấu hình
    // trong Program.cs — SignalR đọc token qua query string "access_token" vì
    // WebSocket handshake của trình duyệt không set được header Authorization tùy ý,
    // xem đoạn OnMessageReceived trong Program.cs).
    // Hub này không có method nào cho client gọi — chỉ dùng 1 chiều Server -> Client (push).
    [Authorize(Roles = "ADMIN")]
    public class AdminNotificationHub : Hub
    {
    }
}
