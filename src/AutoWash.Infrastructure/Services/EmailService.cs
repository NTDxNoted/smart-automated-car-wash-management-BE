using System;
using System.Threading.Tasks;
using AutoWash.Application.DTOs;
using AutoWash.Application.Interfaces;
using AutoWash.Domain.Enums;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AutoWash.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendOtpEmailAsync(string toEmail, string customerName, string code, OtpPurpose purpose)
        {
            var (subject, intro) = GetContent(purpose);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("plain")
            {
                Text = $"Xin chào {customerName},\n\n{intro}\n\nMã xác thực (OTP) của bạn là: {code}\n" +
                       $"Mã có hiệu lực trong ít phút, vui lòng không chia sẻ mã này cho bất kỳ ai.\n\n" +
                       $"Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email.\n\nAutoWash Pro"
            };

            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_settings.SenderEmail, _settings.AppPassword);
                await client.SendAsync(message);
            }
            finally
            {
                if (client.IsConnected)
                    await client.DisconnectAsync(true);
            }

            _logger.LogInformation("OTP email sent to {Email} for purpose {Purpose}", toEmail, purpose);
        }

        private static (string Subject, string Intro) GetContent(OtpPurpose purpose)
        {
            return purpose switch
            {
                OtpPurpose.RegisterVerify => ("Xác thực tài khoản AutoWash Pro", "Cảm ơn bạn đã đăng ký tài khoản. Vui lòng dùng mã bên dưới để xác thực email."),
                OtpPurpose.ResetPassword => ("Đặt lại mật khẩu AutoWash Pro", "Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn."),
                OtpPurpose.Login2Fa => ("Mã xác thực đăng nhập AutoWash Pro", "Vui lòng dùng mã bên dưới để hoàn tất đăng nhập (xác thực 2 lớp)."),
                OtpPurpose.SensitiveAction => ("Mã xác nhận thao tác AutoWash Pro", "Vui lòng dùng mã bên dưới để xác nhận thao tác bạn vừa yêu cầu."),
                OtpPurpose.GuestBookingVerify => ("Xác thực email đặt lịch AutoWash Pro", "Vui lòng dùng mã bên dưới để xác thực email cho lịch đặt xe của bạn."),
                _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, null)
            };
        }
    }
}
