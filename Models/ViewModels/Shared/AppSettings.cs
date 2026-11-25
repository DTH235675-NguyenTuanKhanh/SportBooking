// File: Models/ViewModels/Shared/AppSettings.cs
namespace SportBooking.Models.ViewModels.Shared;

/// <summary>
/// Cấu hình nghiệp vụ đặt sân, bind từ section "BookingSettings" trong appsettings.json.
/// </summary>
public class BookingSettings
{
    public const string SectionName = "BookingSettings";

    /// <summary>Số phút tối đa để khách hoàn tất thanh toán online trước khi booking bị hủy (mặc định 15).</summary>
    public int PaymentTimeoutMinutes { get; set; } = 15;

    /// <summary>Số giờ tối thiểu trước giờ bắt đầu để khách được phép hủy (mặc định 4).</summary>
    public int CancellationMinHoursBefore { get; set; } = 4;

    /// <summary>Chu kỳ chạy của BackgroundService hủy booking quá hạn, tính bằng giây (mặc định 60).</summary>
    public int ExpirationScanIntervalSeconds { get; set; } = 60;

    /// <summary>Số ngày tối đa được đặt trước (mặc định 30 ngày).</summary>
    public int MaxAdvanceBookingDays { get; set; } = 30;

    /// <summary>Cho phép đặt nhiều khung giờ liên tiếp trong 1 lần hay không.</summary>
    public bool AllowMultipleConsecutiveSlots { get; set; } = true;

    /// <summary>Số khung giờ tối đa cho 1 lần đặt.</summary>
    public int MaxSlotsPerBooking { get; set; } = 6;
}

/// <summary>Thông tin ký của merchant cũ để tiếp tục nhận IPN/Return VNPay.</summary>
public class VnPaySettings
{
    public const string SectionName = "VnPaySettings";
    public string TmnCode { get; set; } = string.Empty;
    public string HashSecret { get; set; } = string.Empty;
}

/// <summary>
/// Cấu hình cổng thanh toán MoMo Sandbox, bind từ section "MomoSettings".
/// </summary>
public class MomoSettings
{
    public const string SectionName = "MomoSettings";

    /// <summary>Có bật cổng MoMo hay không.</summary>
    public bool Enabled { get; set; } = false;

    public string PartnerCode { get; set; } = string.Empty;

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string Endpoint { get; set; } = "https://test-payment.momo.vn/v2/gateway/api/create";

    public string ReturnUrl { get; set; } = string.Empty;

    public string IpnUrl { get; set; } = string.Empty;

    public string RequestType { get; set; } = "captureWallet";
}

/// <summary>
/// Cấu hình gửi email, bind từ section "EmailSettings".
/// </summary>
public class EmailSettings
{
    public const string SectionName = "EmailSettings";

    /// <summary>
    /// Chế độ dev: nếu true thì email chỉ được ghi ra log/console thay vì gửi thật.
    /// </summary>
    public bool UseConsoleSender { get; set; } = false;

    public string SmtpHost { get; set; } = string.Empty;

    public int SmtpPort { get; set; } = 587;

    public bool UseSsl { get; set; } = true;

    public string SenderName { get; set; } = "SportBooking";

    public string SenderEmail { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Cấu hình upload file, bind từ section "FileUploadSettings".
/// </summary>
public class FileUploadSettings
{
    public const string SectionName = "FileUploadSettings";

    /// <summary>Dung lượng tối đa cho 1 ảnh, tính bằng byte (mặc định 2MB).</summary>
    public long MaxFileSizeBytes { get; set; } = 2 * 1024 * 1024;

    /// <summary>Danh sách đuôi file được phép upload.</summary>
    public string[] AllowedExtensions { get; set; } = { ".jpg", ".jpeg", ".png", ".webp" };
}
