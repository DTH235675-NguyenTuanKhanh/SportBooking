// File: Models/Entities/Court.cs
namespace SportBooking.Models.Entities;

/// <summary>
/// Sân con (ví dụ: "Sân số 1", "Sân số 2" trong một cụm sân).
/// Mỗi sân thuộc 1 cụm sân và 1 loại sân.
/// </summary>
public class Court
{
    /// <summary>Khóa chính.</summary>
    public int Id { get; set; }

    /// <summary>Khóa ngoại tới cụm sân.</summary>
    public int SportComplexId { get; set; }

    /// <summary>Khóa ngoại tới loại sân.</summary>
    public int SportTypeId { get; set; }

    /// <summary>Tên sân (ví dụ: "Sân số 1 - Sân 5 người").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Mô tả / ghi chú về sân.</summary>
    public string? Description { get; set; }

    /// <summary>Ảnh sân (đường dẫn tương đối, ví dụ: "/uploads/courts/abc.jpg").</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Trạng thái sân: Hoạt động / Bảo trì / Ngừng hoạt động.</summary>
    public CourtStatus Status { get; set; } = CourtStatus.Active;

    /// <summary>
    /// Xóa mềm: khi sân đã có booking thì không xóa cứng, chỉ đánh dấu IsDeleted = true.
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>Ngày tạo.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Ngày cập nhật cuối.</summary>
    public DateTime? UpdatedAt { get; set; }

    // ========== Navigation Properties ==========

    /// <summary>Cụm sân chứa sân này.</summary>
    public SportComplex SportComplex { get; set; } = null!;

    /// <summary>Loại sân của sân này.</summary>
    public SportType SportType { get; set; } = null!;

    /// <summary>Danh sách khung giờ &amp; giá của sân.</summary>
    public ICollection<TimeSlot> TimeSlots { get; set; } = new List<TimeSlot>();

    /// <summary>Danh sách booking của sân.</summary>
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    /// <summary>Danh sách đánh giá về sân.</summary>
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}