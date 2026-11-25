// File: Models/Entities/SportType.cs
namespace SportBooking.Models.Entities;

/// <summary>
/// Loại sân thể thao (Bóng đá mini, Cầu lông, Tennis, Bóng rổ, Pickleball...).
/// Bảng danh mục do Admin quản lý (CRUD).
/// </summary>
public class SportType
{
    /// <summary>Khóa chính.</summary>
    public int Id { get; set; }

    /// <summary>Tên loại sân (ví dụ: "Bóng đá mini 5 người").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Mô tả ngắn.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Icon CSS class (ví dụ: "bi-trophy-fill") hoặc URL ảnh icon.
    /// Dùng để hiển thị trên trang chủ và tìm kiếm.
    /// </summary>
    public string? IconClass { get; set; }

    /// <summary>Thứ tự sắp xếp hiển thị (số càng nhỏ càng lên trước).</summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>Loại sân có đang hoạt động không.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Ngày tạo.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ========== Navigation Properties ==========

    /// <summary>Danh sách các sân thuộc loại này.</summary>
    public ICollection<Court> Courts { get; set; } = new List<Court>();
}
