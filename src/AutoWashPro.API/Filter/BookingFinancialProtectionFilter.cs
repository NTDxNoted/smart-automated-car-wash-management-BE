using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using AutoWash.Application.Interfaces;

namespace AutoWashPro.API.Filters
{
    public class BookingFinancialProtectionFilter : IAsyncActionFilter
    {
        private readonly IApplicationDbContext _context;

        public BookingFinancialProtectionFilter(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // 1. Lấy ID của booking từ URL (thường bạn sẽ đặt tên tham số là 'id' hoặc 'bookingId')
            var idKey = context.ActionArguments.Keys.FirstOrDefault(k => k.ToLower() == "id" || k.ToLower() == "bookingid");

            if (idKey != null && context.ActionArguments[idKey] is int bookingId)
            {
                // 2. Tìm payload (DTO) gửi lên từ Body của request
                var payload = context.ActionArguments.Values.FirstOrDefault(v => v != null && !v.GetType().IsPrimitive && v.GetType() != typeof(string));

                if (payload != null)
                {
                    // 3. Quét xem DTO gửi lên có chứa các trường tài chính bị cấm sửa hay không
                    var payloadType = payload.GetType();
                    bool hasFinancialFields =
                        payloadType.GetProperty("FinalAmount") != null ||
                        payloadType.GetProperty("PointsEarned") != null ||
                        payloadType.GetProperty("DiscountApplied") != null;

                    if (hasFinancialFields)
                    {
                        // 4. Truy vấn DB xem Booking này có tồn tại và đã Completed chưa
                        // Lưu ý: Đảm bảo IApplicationDbContext của bạn đã có DbSet<Booking> Bookings
                        var booking = await _context.Bookings
                            .AsNoTracking()
                            .FirstOrDefaultAsync(b => b.BookingID == bookingId);

                        // Giả định Status "Completed" của bạn có ToString() là "Completed" hoặc một số cụ thể (VD: 3)
                        // Bạn hãy đổi chữ "Completed" dưới đây cho khớp với enum BookingStatus của nhóm nhé
                        if (booking != null && booking.Status.ToString().Equals("Completed", StringComparison.OrdinalIgnoreCase))
                        {
                            // 5. Nếu vi phạm, chặn đứng request và trả về lỗi 403 Forbidden hoặc 400 BadRequest
                            context.Result = new BadRequestObjectResult(new
                            {
                                message = "BR-12: Vi phạm quyền truy cập. Không được phép sửa đổi dữ liệu tài chính (FinalAmount, PointsEarned, DiscountApplied) của giao dịch đã hoàn tất."
                            });

                            return; // Dừng luồng chạy tại đây, không cho đi tiếp vào Controller
                        }
                    }
                }
            }

            // Nếu an toàn (Không sửa trường tài chính, hoặc booking chưa Completed), cho phép request đi tiếp
            await next();
        }
    }
}