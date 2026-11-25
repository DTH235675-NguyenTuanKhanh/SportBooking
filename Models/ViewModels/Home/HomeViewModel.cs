// File: Models/ViewModels/Home/HomeViewModel.cs
namespace SportBooking.Models.ViewModels.Home;

/// <summary>
/// ViewModel cho trang chủ.
/// Bao gồm thanh search nhanh, danh sách loại sân, sân nổi bật, đánh giá nổi bật.
/// </summary>
public class HomeViewModel
{
    /// <summary>Danh sách loại sân (hiển thị dạng icon grid).</summary>
    public List<SportTypeCardViewModel> SportTypes { get; set; } = new();

    /// <summary>Danh sách sân nổi bật (top rating hoặc mới nhất).</summary>
    public List<Court.CourtCardViewModel> FeaturedCourts { get; set; } = new();

    /// <summary>Danh sách đánh giá nổi bật (5 sao + có nhận xét).</summary>
    public List<FeaturedReviewViewModel> FeaturedReviews { get; set; } = new();

    /// <summary>Thống kê tổng quan (số sân, số booking, số khách hàng).</summary>
    public HomeStatsViewModel Stats { get; set; } = new();
}

/// <summary>
/// Card hiển thị loại sân trên trang chủ (dạng icon + tên).
/// </summary>
public class SportTypeCardViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? IconClass { get; set; }
    public string? Description { get; set; }

    /// <summary>Số lượng sân thuộc loại này.</summary>
    public int CourtCount { get; set; }
}

/// <summary>
/// Đánh giá nổi bật hiển thị trên trang chủ.
/// </summary>
public class FeaturedReviewViewModel
{
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerAvatar { get; set; }
    public string CourtName { get; set; } = string.Empty;
    public int CourtId { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Thống kê tổng quan cho trang chủ.
/// </summary>
public class HomeStatsViewModel
{
    /// <summary>Tổng số sân đang hoạt động.</summary>
    public int TotalCourts { get; set; }

    /// <summary>Tổng số booking đã hoàn thành.</summary>
    public int TotalBookings { get; set; }

    /// <summary>Tổng số khách hàng đã đăng ký.</summary>
    public int TotalCustomers { get; set; }

    /// <summary>Tổng số chủ sân.</summary>
    public int TotalOwners { get; set; }
}