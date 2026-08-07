using System.Threading.Tasks;
using AutoWash.Domain.Entities;
using AutoWash.Domain.Enums;

namespace AutoWash.Application.Interfaces
{
    public interface IOtpService
    {
        /// <summary>
        /// Generates a new OTP for the given customer/purpose and emails it.
        /// Throws InvalidOperationException("OTP_COOLDOWN") if requested again too soon.
        /// </summary>
        Task GenerateAndSendAsync(Customer customer, OtpPurpose purpose);

        /// <summary>
        /// Verifies the code for the given customer/purpose. Throws InvalidOperationException
        /// with OTP_NOT_FOUND / OTP_EXPIRED / OTP_LOCKED / OTP_INVALID on failure; returns normally on success.
        /// </summary>
        Task VerifyAsync(int customerId, OtpPurpose purpose, string code);
    }
}
