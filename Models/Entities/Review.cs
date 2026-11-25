// File: Models/Entities/Review.cs
namespace SportBooking.Models.Entities;

/// <summary>
/// Đánh giá của khách hàng cho một lượt đặt sân đã hoàn thành.
/// Quan hệ 1-1 với Booking: mỗi booking chỉ được đánh giá 1 lần.
/// </summary>
public class Review
{
    /// <summary>Khóa chính.</summary>
    public int Id { get; set; }

    /// <summary>Khóa ngoại tới booking (unique - 1 booking chỉ có 1 đánh giá).</summary>
    public int BookingId { get; set; }

    /// <summary>Khóa ngoại tới sân được đánh giá (denormalized để truy vấn nhanh).</summary>
    public int CourtId { get; set; }

    /// <summary>Khóa ngoại tới khách hàng viết đánh giá.</summary>
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>Điểm đánh giá 1-5 sao.</summary>
    public int Rating { get; set; }

    /// <summary>Nhận xét của khách.</summary>
    public string? Comment { get; set; }

    /// <summary>Nội dung phản hồi của chủ sân (nếu có).</summary>
    public string? OwnerReply { get; set; }

    /// <summary>Thời điểm chủ sân phản hồi.</summary>
    public DateTime? OwnerReplyAt { get; set; }

    /// <summary>Đánh giá có đang hiển thị công khai không (Admin có thể ẩn nếu vi phạm).</summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>Ngày tạo đánh giá.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Ngày cập nhật cuối.</summary>
    public DateTime? UpdatedAt { get; set; }

    // ========== Navigation Properties ==========

    /// <summary>Booking được đánh giá.</summary>
    public Booking Booking { get; set; } = null!;

    /// <summary>Sân được đánh giá.</summary>
    public Court Court { get; set; } = null!;

    /// <summary>Khách hàng viết đánh giá.</summary>
    public ApplicationUser Customer { get; set; } = null!;
}