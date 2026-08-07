using System.Threading.Tasks;
using AutoWash.Domain.Enums;

namespace AutoWash.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendOtpEmailAsync(string toEmail, string customerName, string code, OtpPurpose purpose);
    }
}
