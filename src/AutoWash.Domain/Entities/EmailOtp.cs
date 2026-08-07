using System;
using System.ComponentModel.DataAnnotations.Schema;
using AutoWash.Domain.Enums;

namespace AutoWash.Domain.Entities
{
    [Table("email_otp")]
    public class EmailOtp
    {
        public int OtpID { get; set; }
        public int CustomerID { get; set; }
        public string Email { get; set; } = string.Empty;
        public OtpPurpose Purpose { get; set; }
        public string CodeHash { get; set; } = string.Empty;
        public int Attempts { get; set; }
        public bool IsUsed { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
