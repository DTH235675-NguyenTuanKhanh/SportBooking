// File: Models/ViewModels/Court/CourtCardViewModel.cs
namespace SportBooking.Models.ViewModels.Court;

/// <summary>
/// ViewModel cho card hiển thị sân trong kết quả tìm kiếm và danh sách nổi bật.
/// </summary>
public class CourtCardViewModel
{
    /// <summary>ID sân.</summary>
    public int Id { get; set; }

    /// <summary>Tên sân.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Mô tả sân, có thể ghi quy mô sân bóng đá.</summary>
    public string? Description { get; set; }

    /// <summary>Ảnh đại diện sân.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Tên cụm sân.</summary>
    public string ComplexName { get; set; } = string.Empty;

    /// <summary>Địa chỉ đầy đủ (quận, tỉnh).</summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>Tên loại sân.</summary>
    public string SportTypeName { get; set; } = string.Empty;

    /// <summary>Icon loại sân.</summary>
    public string? SportTypeIcon { get; set; }

    /// <summary>Giá thấp nhất (dùng để hiển thị "Giá từ X").</summary>
    public decimal MinPrice { get; set; }

    /// <summary>Giá cao nhất (để hiển thị khoảng giá).</summary>
    public decimal MaxPrice { get; set; }

    /// <summary>Điểm đánh giá trung bình (1-5).</summary>
    public double AverageRating { get; set; }

    /// <summary>Số lượng đánh giá.</summary>
    public int ReviewCount { get; set; }

    /// <summary>Trạng thái sân (Active/Maintenance).</summary>
    public int Status { get; set; }

    /// <summary>Tên trạng thái hiển thị (tiếng Việt).</summary>
    public string StatusText => Status == 0 ? "Hoạt động" : "Bảo trì";

    /// <summary>Số khung giờ còn trống trong ngày (nếu có filter theo ngày).</summary>
    public int AvailableSlotsCount { get; set; }

    /// <summary>Có khung giờ trống trong ngày được lọc hay không.</summary>
    public bool HasAvailableSlots => AvailableSlotsCount > 0;
}
