// File: Services/Implementations/SmtpEmailService.cs
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using SportBooking.Models.ViewModels.Shared;
using SportBooking.Services.Interfaces;

namespace SportBooking.Services.Implementations;

/// <summary>
/// Implementation gửi email thật qua SMTP bằng MailKit.
/// Đọc cấu hình SMTP từ EmailSettings (Host, Port, Username, Password).
/// Chọn class này bằng cách đặt EmailSettings.UseConsoleSender = false.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly EmailSettings _settings;

    public SmtpEmailService(
        ILogger<SmtpEmailService> logger,
        IOptions<EmailSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            // Kết nối và xác thực SMTP.
            // SecureSocketOptions.StartTls phù hợp với hầu hết SMTP server.
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_settings.Username) && !string.IsNullOrWhiteSpace(_settings.Password))
            {
                await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Đã gửi email đến {To} với chủ đề '{Subject}'.", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gửi email đến {To}.", to);
            throw;
        }
    }

    public Task SendPasswordResetEmailAsync(string to, string fullName, string resetLink, CancellationToken cancellationToken = default)
    {
        var subject = "Đặt lại mật khẩu - SportBooking";
        var body = ConsoleEmailService.BuildPasswordResetHtml(fullName, resetLink);
        return SendEmailAsync(to, subject, body, cancellationToken);
    }
}