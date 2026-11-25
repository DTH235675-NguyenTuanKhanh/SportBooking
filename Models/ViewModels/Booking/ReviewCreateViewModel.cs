// File: Models/ViewModels/Booking/ReviewCreateViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace SportBooking.Models.ViewModels.Booking;

/// <summary>
/// ViewModel cho form đánh giá sân sau khi hoàn thành booking.
/// </summary>
public class ReviewCreateViewModel
{
    /// <summary>ID booking được đánh giá.</summary>
    [Required]
    public int BookingId { get; set; }

    /// <summary>Số sao (1-5).</summary>
    [Required(ErrorMessage = "Vui lòng chọn số sao đánh giá.")]
    [Range(1, 5, ErrorMessage = "Số sao phải từ 1 đến 5.")]
    [Display(Name = "Đánh giá của bạn")]
    public int Rating { get; set; }

    /// <summary>Nhận xét (tùy chọn).</summary>
    [Display(Name = "Nhận xét")]
    [MaxLength(1000, ErrorMessage = "Nhận xét tối đa 1000 ký tự.")]
    public string? Comment { get; set; }

    // ===== Thông tin hiển thị =====
    public string CourtName { get; set; } = string.Empty;
    public string? CourtImageUrl { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string TimeRangeText => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
}