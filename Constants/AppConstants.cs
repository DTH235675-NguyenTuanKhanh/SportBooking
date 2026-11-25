// File: Constants/AppConstants.cs
namespace SportBooking.Constants;

/// <summary>
/// Tập trung toàn bộ hằng số dùng chung của hệ thống.
/// Tránh hard-code rải rác trong code (magic string / magic number).
/// </summary>
public static class AppConstants
{
    /// <summary>Tên các role trong hệ thống Identity.</summary>
    public static class Roles
    {
        public const string Customer = "Customer";
        public const string Owner = "Owner";
        public const string Admin = "Admin";

        /// <summary>Danh sách role dùng cho việc seed và kiểm tra.</summary>
        public static readonly string[] All = { Admin, Owner, Customer };
    }

    /// <summary>Khóa cấu hình lấy từ appsettings.json.</summary>
    public static class ConfigKeys
    {
        public const string BookingSettings = "BookingSettings";
        public const string VnPaySettings = "VnPaySettings";
        public const string MomoSettings = "MomoSettings";
        public const string EmailSettings = "EmailSettings";
        public const string FileUploadSettings = "FileUploadSettings";
    }

    /// <summary>Khóa TempData dùng để hiển thị thông báo (toast) giữa các request.</summary>
    public static class TempDataKeys
    {
        public const string Success = "SuccessMessage";
        public const string Error = "ErrorMessage";
        public const string Warning = "WarningMessage";
        public const string Info = "InfoMessage";
    }

    /// <summary>Tiền tố mã đặt sân: SB + yyyyMMdd + '-' + số thứ tự.</summary>
    public const string BookingCodePrefix = "SB";

    /// <summary>Định dạng tiền tệ VNĐ dùng khi hiển thị.</summary>
    public const string CurrencyFormat = "#,##0 ₫";

    /// <summary>Định dạng ngày hiển thị dd/MM/yyyy.</summary>
    public const string DateFormat = "dd/MM/yyyy";

    /// <summary>Định dạng ngày giờ hiển thị.</summary>
    public const string DateTimeFormat = "dd/MM/yyyy HH:mm";

    /// <summary>Số bản ghi mặc định trên mỗi trang.</summary>
    public const int DefaultPageSize = 12;

    /// <summary>Múi giờ Việt Nam dùng để tính toán thời gian nghiệp vụ.</summary>
    public const string VietnamTimeZoneId = "SE Asia Standard Time";
}