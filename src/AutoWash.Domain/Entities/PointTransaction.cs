using System;
using AutoWash.Domain.Enums;

namespace AutoWash.Domain.Entities
{
    public class PointTransaction
    {
        public int PointTxnID { get; set; }
        public int LoyaltyID { get; set; }
        public int Points { get; set; }
        public PointTransactionType Type { get; set; }
        public int? RefBookingID { get; set; }

        // Cột này sẽ chứa thời gian hết hạn gốc khi hoàn trả điểm
        public DateTime? ExpiredAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}