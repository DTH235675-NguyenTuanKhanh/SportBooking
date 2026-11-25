// File: Services/Implementations/ConsoleEmailService.cs
using Microsoft.Extensions.Options;
using SportBooking.Models.ViewModels.Shared;
using SportBooking.Services.Interfaces;

namespace SportBooking.Services.Implementations;

/// <summary>
/// Implementation gửi email dùng khi phát triển:
/// KHÔNG gửi email thật mà chỉ ghi nội dung ra log (ILogger) để lập trình viên
/// lấy được link đặt lại mật khẩu ngay trên console.
/// Chọn class này bằng cách đặt EmailSettings.UseConsoleSender = true.
/// </summary>
public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;
    private readonly EmailSettings _settings;

    public ConsoleEmailService(
        ILogger<ConsoleEmailService> logger,
        IOptions<EmailSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            """
            ==================== [DEV EMAIL - KHÔNG GỬI THẬT] ====================
            From    : {SenderName} <{SenderEmail}>
            To      : {To}
            Subject : {Subject}
            ----------------------------------------------------------------------
            {Body}
            ======================================================================
            """,
            _settings.SenderName,
            _settings.SenderEmail,
            to,
            subject,
            StripHtml(htmlBody));

        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string to, string fullName, string resetLink, CancellationToken cancellationToken = default)
    {
        var subject = "Đặt lại mật khẩu - SportBooking";
        var body = BuildPasswordResetHtml(fullName, resetLink);

        // Với môi trường dev, in link ra log ở dạng text thuần để dễ copy.
        _logger.LogWarning(
            ">>> LINK ĐẶT LẠI MẬT KHẨU cho {Email}: {ResetLink}",
            to,
            resetLink);

        return SendEmailAsync(to, subject, body, cancellationToken);
    }

    /// <summary>
    /// Tạo nội dung HTML của email đặt lại mật khẩu.
    /// Được dùng chung cho cả console và SMTP sender.
    /// </summary>
    internal static string BuildPasswordResetHtml(string fullName, string resetLink)
    {
        return $"""
            <div style="font-family:'Segoe UI',Arial,sans-serif;max-width:600px;margin:0 auto;padding:24px;background:#F8FAFC;">
              <div style="background:#FFFFFF;border-radius:12px;padding:32px;box-shadow:0 2px 8px rgba(15,23,42,0.08);">
                <h2 style="color:#0F172A;margin:0 0 16px;">Đặt lại mật khẩu</h2>
                <p style="color:#475569;line-height:1.6;">Xin chào <strong>{fullName}</strong>,</p>
                <p style="color:#475569;line-height:1.6;">
                  Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản SportBooking của bạn.
                  Vui lòng bấm vào nút bên dưới để tạo mật khẩu mới.
                </p>
                <p style="text-align:center;margin:32px 0;">
                  <a href="{resetLink}"
                     style="background:#16A34A;color:#FFFFFF;text-decoration:none;padding:14px 32px;border-radius:8px;font-weight:600;display:inline-block;">
                    Đặt lại mật khẩu
                  </a>
                </p>
                <p style="color:#475569;line-height:1.6;">
                  Liên kết này sẽ hết hạn sau <strong>24 giờ</strong>. Nếu bạn không yêu cầu đặt lại mật khẩu,
                  hãy bỏ qua email này — mật khẩu hiện tại của bạn vẫn an toàn.
                </p>
                <hr style="border:none;border-top:1px solid #E2E8F0;margin:24px 0;" />
                <p style="color:#94A3B8;font-size:12px;line-height:1.6;word-break:break-all;">
                  Nếu nút trên không hoạt động, hãy sao chép đường dẫn sau vào trình duyệt:<br />
                  {resetLink}
                </p>
              </div>
              <p style="text-align:center;color:#94A3B8;font-size:12px;margin-top:16px;">
                © {DateTime.Now.Year} SportBooking — Hệ thống quản lý đặt sân thể thao
              </p>
            </div>
            """;
    }

    /// <summary>
    /// Loại bỏ thẻ HTML để nội dung log dễ đọc.
    /// </summary>
    private static string StripHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var withoutTags = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");

        // Gộp khoảng trắng liên tiếp và cắt khoảng trắng đầu/cuối.
        return System.Text.RegularExpressions.Regex.Replace(withoutTags, @"\s{2,}", " ").Trim();
    }
}