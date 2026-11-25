// File: Models/ViewModels/Booking/BookingCancelViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace SportBooking.Models.ViewModels.Booking;

/// <summary>
/// ViewModel cho form hủy booking.
/// Bắt buộc nhập lý do hủy theo yêu cầu nghiệp vụ.
/// </summary>
public class BookingCancelViewModel
{
    [Required]
    public int BookingId { get; set; }

    public string BookingCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập lý do hủy đặt sân.")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "Lý do hủy phải có từ 10 đến 500 ký tự.")]
    [Display(Name = "Lý do hủy")]
    public string CancellationReason { get; set; } = string.Empty;

    // ===== Thông tin hiển thị để khách xác nhận lại =====
    public string CourtName { get; set; } = string.Empty;
    public string ComplexName { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string TimeRangeText => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
    public decimal TotalAmount { get; set; }
    public int PaymentStatus { get; set; }

    /// <summary>Cảnh báo hoàn tiền (nếu đã thanh toán online).</summary>
    public bool WillRefund => PaymentStatus == 1; // Paid

    /// <summary>Số giờ tối thiểu trước giờ bắt đầu để được hủy (từ cấu hình).</summary>
    public int MinHoursBefore { get; set; } = 4;
}