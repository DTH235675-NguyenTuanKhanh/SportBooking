// File: Models/ViewModels/Court/CourtDetailViewModel.cs
namespace SportBooking.Models.ViewModels.Court;

/// <summary>
/// ViewModel cho trang chi tiết sân.
/// Bao gồm thông tin sân, cụm sân, bảng giá, đánh giá và các khung giờ.
/// </summary>
public class CourtDetailViewModel
{
    // ===== Thông tin sân =====
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int Status { get; set; }
    public string StatusText => Status switch { 0 => "Hoạt động", 1 => "Bảo trì", _ => "Ngừng hoạt động" };

    // ===== Thông tin cụm sân =====
    public int SportComplexId { get; set; }
    public string ComplexName { get; set; } = string.Empty;
    public string ComplexAddress { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string? ComplexDescription { get; set; }
    public string? ComplexPhone { get; set; }
    public string? ComplexEmail { get; set; }
    public TimeSpan? OpeningTime { get; set; }
    public TimeSpan? ClosingTime { get; set; }

    /// <summary>Giờ mở cửa dạng chuỗi hiển thị (vd: "06:00 - 23:00").</summary>
    public string OpeningHoursText =>
        OpeningTime.HasValue && ClosingTime.HasValue
            ? $"{OpeningTime.Value:hh\\:mm} - {ClosingTime.Value:hh\\:mm}"
            : "Chưa cập nhật";

    // ===== Loại sân =====
    public string SportTypeName { get; set; } = string.Empty;
    public string? SportTypeIcon { get; set; }

    // ===== Giá =====
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }

    // ===== Đánh giá =====
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }

    /// <summary>Phân bố số lượng đánh giá theo số sao (5 -> 1).</summary>
    public Dictionary<int, int> RatingDistribution { get; set; } = new();

    /// <summary>Danh sách đánh giá (có phân trang).</summary>
    public List<ReviewItemViewModel> Reviews { get; set; } = new();

    /// <summary>Tổng số đánh giá để phân trang.</summary>
    public int TotalReviews { get; set; }

    /// <summary>Trang đánh giá hiện tại.</summary>
    public int ReviewPage { get; set; } = 1;

    /// <summary>Số đánh giá mỗi trang.</summary>
    public int ReviewPageSize { get; set; } = 5;

    /// <summary>Tổng số trang đánh giá.</summary>
    public int TotalReviewPages => ReviewPageSize > 0
        ? (int)Math.Ceiling((double)TotalReviews / ReviewPageSize)
        : 0;

    // ===== Bảng giá theo khung giờ =====
    public List<TimeSlotPriceViewModel> TimeSlotPrices { get; set; } = new();

    // ===== Gallery ảnh =====
    /// <summary>
    /// Danh sách ảnh của sân (hiện tại chỉ có 1 ảnh chính; có thể mở rộng thành nhiều ảnh).
    /// </summary>
    public List<string> GalleryImages { get; set; } = new();

    // ===== Sân khác cùng cụm =====
    /// <summary>Các sân khác trong cùng cụm sân (để gợi ý).</summary>
    public List<CourtCardViewModel> OtherCourtsInComplex { get; set; } = new();

    /// <summary>Sân có thể đặt được hay không (đang hoạt động).</summary>
    public bool ComplexIsActive { get; set; } = true;
    public bool OwnerCanReceiveBookings { get; set; } = true;
    public bool SportTypeIsActive { get; set; } = true;
    public bool CanBook => Status == 0 && ComplexIsActive && OwnerCanReceiveBookings && SportTypeIsActive;
    public string UnavailableReason => Status == 1
        ? "Sân đang bảo trì, tạm thời chưa nhận lịch đặt mới."
        : Status != 0
            ? "Sân đã ngừng hoạt động và không nhận lịch đặt mới."
            : !ComplexIsActive
            ? "Cụm sân đang tạm ngừng hoạt động và chưa nhận lịch đặt mới."
            : !SportTypeIsActive
                ? "Loại sân này đang ngừng nhận lịch đặt mới."
                : "Chủ sân hiện chưa thể nhận lịch đặt mới. Vui lòng chọn sân khác.";
}

/// <summary>
/// ViewModel cho một khung giờ và giá tương ứng (hiển thị bảng giá).
/// </summary>
public class TimeSlotPriceViewModel
{
    public int Id { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public decimal PriceWeekday { get; set; }
    public decimal PriceWeekend { get; set; }
    public decimal PriceHoliday { get; set; }

    /// <summary>Chuỗi hiển thị khung giờ (vd: "18:00 - 19:00").</summary>
    public string TimeRangeText => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
}

/// <summary>
/// ViewModel cho một đánh giá hiển thị trên trang chi tiết sân.
/// </summary>
public class ReviewItemViewModel
{
    public int Id { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerAvatar { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Phản hồi của chủ sân (nếu có).</summary>
    public string? OwnerReply { get; set; }
    public DateTime? OwnerReplyAt { get; set; }
}
