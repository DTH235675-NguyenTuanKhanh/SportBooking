// File: Models/Entities/SportComplex.cs
namespace SportBooking.Models.Entities;

/// <summary>
/// Cụm sân thể thao (ví dụ: "Sân vận động Thể Thao ABC", "Cụm sân Hoàng Gia").
/// Một chủ sân (Owner) có thể sở hữu nhiều cụm sân.
/// Mỗi cụm sân chứa nhiều sân con (Court).
/// </summary>
public class SportComplex
{
    /// <summary>Khóa chính.</summary>
    public int Id { get; set; }

    /// <summary>Khóa ngoại tới chủ sân (Owner).</summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>Tên cụm sân.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Địa chỉ chi tiết.</summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>Tỉnh / Thành phố.</summary>
    public string Province { get; set; } = string.Empty;

    /// <summary>Quận / Huyện.</summary>
    public string District { get; set; } = string.Empty;

    /// <summary>Mô tả cụm sân (giới thiệu, tiện ích...).</summary>
    public string? Description { get; set; }

    /// <summary>Ảnh đại diện cụm sân (logo hoặc ảnh tổng quan).</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Số điện thoại liên hệ.</summary>
    public string? PhoneNumber { get; set; }

    /// <summary>Email liên hệ.</summary>
    public string? Email { get; set; }

    /// <summary>Giờ mở cửa (ví dụ: "06:00").</summary>
    public TimeSpan? OpeningTime { get; set; }

    /// <summary>Giờ đóng cửa (ví dụ: "23:00").</summary>
    public TimeSpan? ClosingTime { get; set; }

    /// <summary>Cụm sân có đang hoạt động không.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Ngày tạo.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Ngày cập nhật cuối.</summary>
    public DateTime? UpdatedAt { get; set; }

    // ========== Navigation Properties ==========

    /// <summary>Chủ sân sở hữu cụm sân này.</summary>
    public ApplicationUser Owner { get; set; } = null!;

    /// <summary>Danh sách các sân con trong cụm.</summary>
    public ICollection<Court> Courts { get; set; } = new List<Court>();
}
