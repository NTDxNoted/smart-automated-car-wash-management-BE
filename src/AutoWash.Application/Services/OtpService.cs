using System;
using System.Linq;
using System.Threading.Tasks;
using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Entities;
using AutoWash.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AutoWash.Application.Services
{
    public class OtpService : IOtpService
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IEmailService _emailService;
        private readonly OtpSettings _settings;

        public OtpService(IApplicationDbContext dbContext, IEmailService emailService, IOptions<OtpSettings> settings)
        {
            _dbContext = dbContext;
            _emailService = emailService;
            _settings = settings.Value;
        }

        public async Task GenerateAndSendAsync(Customer customer, OtpPurpose purpose)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var now = DateTime.UtcNow;

            var activeOtps = await _dbContext.EmailOtps
                .Where(o => o.CustomerID == customer.CustomerID && o.Purpose == purpose && !o.IsUsed)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var lastOtp = activeOtps.FirstOrDefault();
            if (lastOtp != null && (now - lastOtp.CreatedAt).TotalSeconds < _settings.ResendCooldownSeconds)
                throw new InvalidOperationException("OTP_COOLDOWN");

            // Chỉ mã OTP mới nhất mới còn hiệu lực — vô hiệu hoá các mã cũ chưa dùng của cùng purpose.
            foreach (var otp in activeOtps)
                otp.IsUsed = true;

            var code = GenerateCode();

            _dbContext.EmailOtps.Add(new EmailOtp
            {
                CustomerID = customer.CustomerID,
                Email = customer.Email ?? string.Empty,
                Purpose = purpose,
                CodeHash = BCrypt.Net.BCrypt.HashPassword(code),
                Attempts = 0,
                IsUsed = false,
                ExpiresAt = now.AddMinutes(_settings.ExpiryMinutes),
                CreatedAt = now
            });

            await _dbContext.SaveChangesAsync();

            await _emailService.SendOtpEmailAsync(customer.Email ?? string.Empty, customer.FullName, code, purpose);
        }

        public async Task VerifyAsync(int customerId, OtpPurpose purpose, string code)
        {
            var otp = await _dbContext.EmailOtps
                .Where(o => o.CustomerID == customerId && o.Purpose == purpose && !o.IsUsed)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otp == null)
                throw new InvalidOperationException("OTP_NOT_FOUND");

            if (otp.ExpiresAt < DateTime.UtcNow)
            {
                otp.IsUsed = true;
                await _dbContext.SaveChangesAsync();
                throw new InvalidOperationException("OTP_EXPIRED");
            }

            if (otp.Attempts >= _settings.MaxAttempts)
            {
                otp.IsUsed = true;
                await _dbContext.SaveChangesAsync();
                throw new InvalidOperationException("OTP_LOCKED");
            }

            if (!BCrypt.Net.BCrypt.Verify(code ?? string.Empty, otp.CodeHash))
            {
                otp.Attempts++;
                if (otp.Attempts >= _settings.MaxAttempts)
                    otp.IsUsed = true;
                await _dbContext.SaveChangesAsync();
                throw new InvalidOperationException("OTP_INVALID");
            }

            otp.IsUsed = true;
            await _dbContext.SaveChangesAsync();
        }

        private static string GenerateCode()
        {
            return System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        }
    }
}
