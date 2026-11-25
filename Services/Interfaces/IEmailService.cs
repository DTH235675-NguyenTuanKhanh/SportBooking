// File: Services/Interfaces/IEmailService.cs
namespace SportBooking.Services.Interfaces;

/// <summary>
/// Trừu tượng hóa việc gửi email.
/// Có 2 implementation:
/// - ConsoleEmailService: chỉ ghi nội dung email ra log (dùng khi phát triển).
/// - SmtpEmailService: gửi thật qua SMTP bằng MailKit.
/// Lựa chọn được cấu hình bằng EmailSettings.UseConsoleSender.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Gửi email dạng HTML.
    /// </summary>
    /// <param name="to">Địa chỉ email người nhận.</param>
    /// <param name="subject">Tiêu đề email.</param>
    /// <param name="htmlBody">Nội dung HTML của email.</param>
    /// <param name="cancellationToken">Token hủy tác vụ.</param>
    Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gửi email đặt lại mật khẩu chứa link có token.
    /// </summary>
    /// <param name="to">Email người nhận.</param>
    /// <param name="fullName">Tên người nhận để chào trong nội dung.</param>
    /// <param name="resetLink">Link đặt lại mật khẩu đầy đủ (đã chứa token).</param>
    /// <param name="cancellationToken">Token hủy tác vụ.</param>
    Task SendPasswordResetEmailAsync(string to, string fullName, string resetLink, CancellationToken cancellationToken = default);
}