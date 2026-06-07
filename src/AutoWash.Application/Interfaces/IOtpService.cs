namespace AutoWash.Application.Interfaces
{
    public interface IOtpService
    {
        string GenerateAndStore(string phone);
        bool Verify(string phone, string otpCode);
    }
}
