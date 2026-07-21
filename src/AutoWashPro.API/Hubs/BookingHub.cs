using Microsoft.AspNetCore.SignalR;

namespace AutoWashPro.API.Hubs
{
    // SignalR Hub công khai (không [Authorize]) — trang đặt lịch cho cả Guest lẫn Member
    // đều cần nhận cập nhật số chỗ trống theo thời gian thực, thay vì polling liên tục.
    // Hub này không có method nào cho client gọi — chỉ dùng 1 chiều Server -> Client (push).
    public class BookingHub : Hub
    {
    }
}
