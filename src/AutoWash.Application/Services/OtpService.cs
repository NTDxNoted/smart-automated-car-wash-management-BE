using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using AutoWash.Application.Interfaces;

namespace AutoWash.Application.Services
{
    // Singleton: dictionary tồn tại suốt vòng đời app (BR-10: OTP mô phỏng)
    public class OtpService : IOtpService
    {
        private readonly ILogger<OtpService> _logger;
        private readonly ConcurrentDictionary<string, (string Code, DateTime ExpiresAt)> _store = new();

        public OtpService(ILogger<OtpService> logger)
        {
            _logger = logger;
        }

        public string GenerateAndStore(string phone)
        {
            var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            _store[phone] = (otp, DateTime.UtcNow.AddMinutes(5));
            _logger.LogInformation("[OTP-SIM] Phone={Phone} OTP={OTP} ExpiresAt={ExpiresAt}", phone, otp, DateTime.UtcNow.AddMinutes(5));
            return otp;
        }

        public bool Verify(string phone, string otpCode)
        {
            if (!_store.TryGetValue(phone, out var entry)) return false;
            if (DateTime.UtcNow > entry.ExpiresAt) { _store.TryRemove(phone, out _); return false; }
            if (entry.Code != otpCode) return false;
            _store.TryRemove(phone, out _);
            return true;
        }
    }
}
