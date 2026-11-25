// File: Models/ViewModels/Booking/BookingCreateViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace SportBooking.Models.ViewModels.Booking;

/// <summary>
/// ViewModel cho trang tạo booking (chọn khung giờ và đặt sân).
/// </summary>
public class BookingCreateViewModel
{
    // ===== Thông tin sân đã chọn (readonly, hiển thị trong summary) =====
    public int CourtId { get; set; }
    public string CourtName { get; set; } = string.Empty;
    public string ComplexName { get; set; } = string.Empty;
    public string ComplexAddress { get; set; } = string.Empty;
    public string SportTypeName { get; set; } = string.Empty;
    public string? CourtImageUrl { get; set; }

    // ===== Ngày đặt sân =====
    [Required(ErrorMessage = "Vui lòng chọn ngày đặt sân.")]
    [Display(Name = "Ngày đặt sân")]
    [DataType(DataType.Date)]
    public DateTime BookingDate { get; set; } = SportBooking.Helpers.ScheduleGuard.Now.Date.AddDays(1);

    // ===== Các khung giờ đã chọn (multiple selection) =====
    [Required(ErrorMessage = "Vui lòng chọn ít nhất một khung giờ.")]
    [MinLength(1, ErrorMessage = "Vui lòng chọn ít nhất một khung giờ.")]
    public List<int> SelectedTimeSlotIds { get; set; } = new();

    public Dictionary<int, string> SlotQuotes { get; set; } = new();

    // ===== Phương thức thanh toán =====
    [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
    [Display(Name = "Phương thức thanh toán")]
    [RegularExpression("^(BankTransfer|Counter)$", ErrorMessage = "Phương thức thanh toán không hợp lệ.")]
    public string PaymentMethod { get; set; } = "Counter";

    public bool TransferAvailable { get; set; }

    // ===== Ghi chú (tùy chọn) =====
    [Display(Name = "Ghi chú")]
    [MaxLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự.")]
    public string? Notes { get; set; }

    // ===== Dữ liệu cho View =====

    /// <summary>Danh sách tất cả khung giờ của sân, với trạng thái trống/đã đặt/bảo trì.</summary>
    public List<TimeSlotAvailabilityViewModel> TimeSlots { get; set; } = new();

    /// <summary>Tổng tiền tạm tính (client-side tính, server validate lại).</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Ngày tối thiểu có thể đặt (hôm nay + 1).</summary>
    public DateTime MinBookingDate => SportBooking.Helpers.ScheduleGuard.Now.Date;

    /// <summary>Ngày tối đa có thể đặt (30 ngày sau).</summary>
    public DateTime MaxBookingDate => SportBooking.Helpers.ScheduleGuard.Now.Date.AddDays(30);
}

/// <summary>
/// Trạng thái khả dụng của một khung giờ trong ngày được chọn.
/// </summary>
public class TimeSlotAvailabilityViewModel
{
    public string Quote { get; set; } = string.Empty;
    public int TimeSlotId { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int DurationMinutes { get; set; }

    /// <summary>Giá khung giờ theo loại ngày (đã tính theo BookingDate).</summary>
    public decimal Price { get; set; }

    /// <summary>Trạng thái: Available, Booked, Maintenance, Past.</summary>
    public TimeSlotStatus Status { get; set; }

    /// <summary>Chuỗi hiển thị khung giờ (vd: "07:00 - 08:00").</summary>
    public string TimeRangeText => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";

    /// <summary>CSS class cho trạng thái (để hiển thị màu khác nhau).</summary>
    public string StatusClass => Status switch
    {
        TimeSlotStatus.Available => "timeslot-available",
        TimeSlotStatus.Booked => "timeslot-booked",
        TimeSlotStatus.Maintenance => "timeslot-maintenance",
        TimeSlotStatus.Past => "timeslot-past",
        _ => ""
    };

    /// <summary>Có thể chọn được hay không.</summary>
    public bool IsSelectable => Status == TimeSlotStatus.Available;
}

/// <summary>
/// Enum trạng thái khung giờ (trong ngày cụ thể).
/// </summary>
public enum TimeSlotStatus
{
    Available = 0,    // Trống, có thể đặt
    Booked = 1,       // Đã được đặt
    Maintenance = 2,  // Sân đang bảo trì
    Past = 3          // Khung giờ đã qua
}
