using System;

namespace AutoWash.Application.Common
{
    // Không phải lỗi thật — dùng để báo AuthController rằng mật khẩu đã đúng nhưng
    // còn thiếu bước xác thực OTP (2FA) trước khi có thể cấp JWT.
    public class TwoFactorRequiredException : Exception
    {
        public string MaskedEmail { get; }

        public TwoFactorRequiredException(string maskedEmail) : base("OTP_REQUIRED")
        {
            MaskedEmail = maskedEmail;
        }
    }
}
