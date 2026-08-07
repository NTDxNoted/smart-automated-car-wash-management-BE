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
    public class GuestOtpService : IGuestOtpService
    {
        // Một email được coi là "vừa xác thực" trong khoảng thời gian này khi tạo booking —
        // đủ để hoàn tất Step 2 -> Step 3 -> Submit, không cần cấu hình riêng.
        private static readonly TimeSpan VerifiedWindow = TimeSpan.FromMinutes(30);

        private readonly IApplicationDbContext _dbContext;
        private readonly IEmailService _emailService;
        private readonly OtpSettings _settings;

        public GuestOtpService(IApplicationDbContext dbContext, IEmailService emailService, IOptions<OtpSettings> settings)
        {
            _dbContext = dbContext;
            _emailService = emailService;
            _settings = settings.Value;
        }

        public async Task GenerateAndSendAsync(string email, OtpPurpose purpose)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email không được để trống");

            var normalizedEmail = email.Trim().ToLowerInvariant();
            var now = DateTime.UtcNow;

            var activeOtps = await _dbContext.GuestEmailOtps
                .Where(o => o.Email == normalizedEmail && o.Purpose == purpose && !o.IsUsed)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var lastOtp = activeOtps.FirstOrDefault();
            if (lastOtp != null && (now - lastOtp.CreatedAt).TotalSeconds < _settings.ResendCooldownSeconds)
                throw new InvalidOperationException("OTP_COOLDOWN");

            foreach (var otp in activeOtps)
                otp.IsUsed = true;

            var code = GenerateCode();

            _dbContext.GuestEmailOtps.Add(new GuestEmailOtp
            {
                Email = normalizedEmail,
                Purpose = purpose,
                CodeHash = BCrypt.Net.BCrypt.HashPassword(code),
                Attempts = 0,
                IsUsed = false,
                ExpiresAt = now.AddMinutes(_settings.ExpiryMinutes),
                CreatedAt = now
            });

            await _dbContext.SaveChangesAsync();

            var displayName = normalizedEmail.Split('@')[0];
            await _emailService.SendOtpEmailAsync(normalizedEmail, displayName, code, purpose);
        }

        public async Task VerifyAsync(string email, OtpPurpose purpose, string code)
        {
            var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();

            var otp = await _dbContext.GuestEmailOtps
                .Where(o => o.Email == normalizedEmail && o.Purpose == purpose && !o.IsUsed)
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
            otp.VerifiedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> IsRecentlyVerifiedAsync(string email, OtpPurpose purpose)
        {
            var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
            var cutoff = DateTime.UtcNow - VerifiedWindow;

            return await _dbContext.GuestEmailOtps.AnyAsync(o =>
                o.Email == normalizedEmail &&
                o.Purpose == purpose &&
                o.IsUsed &&
                o.VerifiedAt != null &&
                o.VerifiedAt >= cutoff);
        }

        private static string GenerateCode()
        {
            return System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        }
    }
}
