using System;
using AutoWash.Domain.Enums;

namespace AutoWash.Domain.Entities
{
    public class Transaction
    {
        public int TransactionID { get; set; }
        public int BookingID { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public DateTime? PaidAt { get; set; }
        public TransactionStatus Status { get; set; }
    }
}
