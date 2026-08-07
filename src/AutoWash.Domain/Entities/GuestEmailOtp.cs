using System;
using System.ComponentModel.DataAnnotations.Schema;
using AutoWash.Domain.Enums;

namespace AutoWash.Domain.Entities
{
    // OTP xác thực email cho khách vãng lai (Guest) khi đặt lịch — không gắn với Customer nào,
    // vì mọi Guest hiện đang dùng chung 1 Customer "Khách vãng lai" (xem BookingService.CreateBookingAsync).
    [Table("guest_email_otp")]
    public class GuestEmailOtp
    {
        public int OtpID { get; set; }
        public string Email { get; set; } = string.Empty;
        public OtpPurpose Purpose { get; set; }
        public string CodeHash { get; set; } = string.Empty;
        public int Attempts { get; set; }
        public bool IsUsed { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
