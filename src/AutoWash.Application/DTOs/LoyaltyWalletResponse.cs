using System;
using System.Collections.Generic;

namespace AutoWash.Application.DTOs
{
    public class LoyaltyWalletResponse
    {
        public int CustomerId { get; set; }
        public int TotalPoints { get; set; }
        public bool CanRedeem { get; set; }
        public IEnumerable<PointBatchDto> Batches { get; set; } = new List<PointBatchDto>();
    }

    public class PointBatchDto
    {
        public int Points { get; set; }
        public DateTime EarnedAt { get; set; }
        public DateTime ExpiredAt { get; set; }
        public int DaysUntilExpiry { get; set; }
    }
}
