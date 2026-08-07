using System.Threading.Tasks;
using AutoWash.Domain.Enums;

namespace AutoWash.Application.Interfaces
{
    // Xác thực email không cần tài khoản — dùng cho khách vãng lai (Guest) khi đặt lịch.
    public interface IGuestOtpService
    {
        /// <summary>
        /// Sinh OTP mới cho email và gửi. Throws InvalidOperationException("OTP_COOLDOWN") nếu gọi lại quá sớm.
        /// </summary>
        Task GenerateAndSendAsync(string email, OtpPurpose purpose);

        /// <summary>
        /// Xác thực mã OTP. Throws InvalidOperationException với OTP_NOT_FOUND / OTP_EXPIRED /
        /// OTP_LOCKED / OTP_INVALID khi thất bại; trả về bình thường khi thành công.
        /// </summary>
        Task VerifyAsync(string email, OtpPurpose purpose, string code);

        /// <summary>
        /// Kiểm tra email này đã xác thực OTP thành công gần đây chưa (dùng ở bước tạo booking
        /// để không tin tưởng mù quáng cờ emailVerified do client tự gửi lên).
        /// </summary>
        Task<bool> IsRecentlyVerifiedAsync(string email, OtpPurpose purpose);
    }
}
