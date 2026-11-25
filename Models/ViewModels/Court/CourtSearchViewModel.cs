// File: Models/ViewModels/Court/CourtSearchViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace SportBooking.Models.ViewModels.Court;

/// <summary>
/// ViewModel cho trang tìm kiếm sân.
/// </summary>
public class CourtSearchViewModel
{
    /// <summary>
    /// Từ khóa tìm kiếm (tên sân, tên cụm sân, mô tả).
    /// </summary>
    [Display(Name = "Từ khóa")]
    [MaxLength(200)]
    public string? Keyword { get; set; }

    /// <summary>
    /// Lọc theo loại sân (SportTypeId).
    /// </summary>
    [Display(Name = "Loại sân")]
    public int? SportTypeId { get; set; }

    /// <summary>
    /// Lọc theo tỉnh/thành phố.
    /// </summary>
    [Display(Name = "Tỉnh/Thành phố")]
    [MaxLength(100)]
    public string? Province { get; set; }

    /// <summary>
    /// Lọc theo quận/huyện.
    /// </summary>
    [Display(Name = "Quận/Huyện")]
    [MaxLength(100)]
    public string? District { get; set; }

    /// <summary>
    /// Lọc theo ngày đặt sân (để kiểm tra khung giờ trống).
    /// </summary>
    [Display(Name = "Ngày")]
    [DataType(DataType.Date)]
    public DateTime? BookingDate { get; set; }

    /// <summary>
    /// Lọc theo khung giờ (TimeSlotId) - chỉ hiển thị sân có khung giờ này trống.
    /// </summary>
    [Display(Name = "Khung giờ")]
    public int? TimeSlotId { get; set; }

    /// <summary>
    /// Giá tối thiểu (VNĐ).
    /// </summary>
    [Display(Name = "Giá từ")]
    [Range(0, 10000000)]
    public decimal? MinPrice { get; set; }

    /// <summary>
    /// Giá tối đa (VNĐ).
    /// </summary>
    [Display(Name = "Giá đến")]
    [Range(0, 10000000)]
    public decimal? MaxPrice { get; set; }

    /// <summary>
    /// Sắp xếp: price_asc, price_desc, rating_desc, newest.
    /// </summary>
    [Display(Name = "Sắp xếp")]
    public string SortBy { get; set; } = "rating_desc";

    /// <summary>
    /// Trang hiện tại (phân trang).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Số kết quả mỗi trang.
    /// </summary>
    public int PageSize { get; set; } = 12;

    // ===== Dữ liệu trả về cho View =====

    /// <summary>
    /// Danh sách sân tìm được.
    /// </summary>
    public List<CourtCardViewModel> Courts { get; set; } = new();

    /// <summary>
    /// Tổng số kết quả.
    /// </summary>
    public int TotalResults { get; set; }

    /// <summary>
    /// Tổng số trang.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalResults / PageSize);

    /// <summary>
    /// Danh sách loại sân (cho dropdown filter).
    /// </summary>
    public List<SportTypeOption> SportTypes { get; set; } = new();

    /// <summary>
    /// Danh sách tỉnh/thành (cho dropdown filter).
    /// </summary>
    public List<string> Provinces { get; set; } = new();

    /// <summary>
    /// Danh sách khung giờ (cho dropdown filter).
    /// </summary>
    public List<TimeSlotOption> TimeSlots { get; set; } = new();
}

/// <summary>
/// Option cho dropdown loại sân.
/// </summary>
public class SportTypeOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? IconClass { get; set; }
}

/// <summary>
/// Option cho dropdown khung giờ.
/// </summary>
public class TimeSlotOption
{
    public int Id { get; set; }
    public string DisplayText { get; set; } = string.Empty; // Ví dụ: "07:00 - 08:00"
}