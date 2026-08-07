namespace AutoWash.Application.DTOs
{
    public class OtpSettings
    {
        public int ExpiryMinutes { get; set; } = 5;
        public int MaxAttempts { get; set; } = 5;
        public int ResendCooldownSeconds { get; set; } = 60;
    }
}
