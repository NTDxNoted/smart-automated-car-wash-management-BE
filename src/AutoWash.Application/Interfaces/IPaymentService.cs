using System.Threading.Tasks;
using AutoWash.Application.DTOs.Admin;
using AutoWash.Domain.Entities;

namespace AutoWash.Application.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentResponse> RecordPaymentAsync(int bookingId, RecordPaymentRequest request);
        Task<Transaction> GetTransactionByBookingIdAsync(int bookingId);
    }
}
