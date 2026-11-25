// File: Models/Entities/Notification.cs
namespace SportBooking.Models.Entities;

/// <summary>
/// Thông báo trong ứng dụng gửi cho người dùng
/// (ví dụ: booking được xác nhận, booking bị hủy do quá hạn, có phản hồi đánh giá...).
/// </summary>
public class Notification
{
    /// <summary>Khóa chính.</summary>
    public int Id { get; set; }

    /// <summary>Khóa ngoại tới người nhận.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Tiêu đề thông báo.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Nội dung thông báo.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Đường dẫn điều hướng khi người dùng bấm vào thông báo.</summary>
    public string? Url { get; set; }

    /// <summary>Loại thông báo (dùng để chọn icon/màu): "booking", "payment", "review", "system".</summary>
    public string Type { get; set; } = "system";

    /// <summary>Đã đọc chưa.</summary>
    public bool IsRead { get; set; } = false;

    /// <summary>Thời điểm đọc.</summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>Ngày tạo thông báo.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ========== Navigation Properties ==========

    /// <summary>Người nhận thông báo.</summary>
    public ApplicationUser User { get; set; } = null!;
}