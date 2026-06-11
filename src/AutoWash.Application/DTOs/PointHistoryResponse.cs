using System;

namespace AutoWash.Application.DTOs
{
    public class PointHistoryResponse
    {
        public int TxnId { get; set; }
        public string Type { get; set; } = string.Empty;
        public int Points { get; set; }
        public int? RefBookingId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiredAt { get; set; }
    }
}
