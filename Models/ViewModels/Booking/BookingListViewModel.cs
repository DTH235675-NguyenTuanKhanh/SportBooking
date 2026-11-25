// File: Models/ViewModels/Booking/BookingListViewModel.cs
namespace SportBooking.Models.ViewModels.Booking;

/// <summary>
/// ViewModel cho trang lịch sử đặt sân của khách hàng.
/// </summary>
public class BookingListViewModel
{
    /// <summary>Danh sách booking.</summary>
    public List<BookingListItemViewModel> Bookings { get; set; } = new();

    /// <summary>Bộ lọc theo trạng thái (null = tất cả).</summary>
    public string? StatusFilter { get; set; }

    /// <summary>Lọc từ ngày.</summary>
    public DateTime? FromDate { get; set; }

    /// <summary>Lọc đến ngày.</summary>
    public DateTime? ToDate { get; set; }

    /// <summary>Từ khóa tìm kiếm (mã booking, tên sân).</summary>
    public string? Keyword { get; set; }

    /// <summary>Trang hiện tại.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Số bản ghi mỗi trang.</summary>
    public int PageSize { get; set; } = 10;

    /// <summary>Tổng số bản ghi.</summary>
    public int TotalCount { get; set; }

    /// <summary>Tổng số trang.</summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>Số lượng booking theo từng trạng thái (để hiển thị tab badges).</summary>
    public Dictionary<string, int> StatusCounts { get; set; } = new();
}

/// <summary>
/// ViewModel cho một dòng booking trong danh sách.
/// </summary>
public class BookingListItemViewModel
{
    public int Id { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string BookingGroupCode { get; set; } = string.Empty;
    public string? BankTransferGroupCode { get; set; }

    public int CourtId { get; set; }
    public string CourtName { get; set; } = string.Empty;
    public string? CourtImageUrl { get; set; }
    public string ComplexName { get; set; } = string.Empty;
    public string ComplexAddress { get; set; } = string.Empty;

    public DateTime BookingDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string TimeRangeText => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";

    public decimal TotalAmount { get; set; }

    public int Status { get; set; }
    public int PaymentStatus { get; set; }
    public string PaymentMethodText { get; set; } = string.Empty;

    /// <summary>Tên trạng thái hiển thị (tiếng Việt).</summary>
    public string StatusText { get; set; } = string.Empty;

    /// <summary>CSS class cho badge trạng thái.</summary>
    public string StatusBadgeClass { get; set; } = string.Empty;

    /// <summary>Tên trạng thái thanh toán hiển thị.</summary>
    public string PaymentStatusText { get; set; } = string.Empty;

    /// <summary>Thời điểm tạo booking.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Có thể hủy hay không (Confirmed và còn >= 4 giờ trước giờ bắt đầu).</summary>
    public bool CanCancel { get; set; }

    /// <summary>Có thể đánh giá hay không (Completed và chưa đánh giá).</summary>
    public bool CanReview { get; set; }

    /// <summary>Đã đánh giá chưa.</summary>
    public bool HasReview { get; set; }

    /// <summary>Lý do không thể hủy (hiển thị tooltip).</summary>
    public string? CancelDisabledReason { get; set; }

    /// <summary>Hạn thanh toán (với booking Pending + Online).</summary>
    public DateTime? PaymentDeadline { get; set; }

    /// <summary>Thời gian còn lại để thanh toán (phút).</summary>
    public int? MinutesLeftToPay =>
        PaymentDeadline.HasValue && PaymentDeadline.Value > SportBooking.Helpers.ScheduleGuard.Now
            ? (int)Math.Ceiling((PaymentDeadline.Value - SportBooking.Helpers.ScheduleGuard.Now).TotalMinutes)
            : null;
}
