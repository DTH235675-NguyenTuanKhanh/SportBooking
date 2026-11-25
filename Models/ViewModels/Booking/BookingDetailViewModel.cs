// File: Models/ViewModels/Booking/BookingDetailViewModel.cs
namespace SportBooking.Models.ViewModels.Booking;

/// <summary>
/// ViewModel cho trang chi tiết một booking.
/// </summary>
public class BookingDetailViewModel
{
    public int Id { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string BookingGroupCode { get; set; } = string.Empty;
    public string? BankTransferGroupCode { get; set; }

    // ===== Thông tin sân =====
    public int CourtId { get; set; }
    public string CourtName { get; set; } = string.Empty;
    public string? CourtImageUrl { get; set; }
    public string ComplexName { get; set; } = string.Empty;
    public string ComplexAddress { get; set; } = string.Empty;
    public string ComplexPhone { get; set; } = string.Empty;
    public string SportTypeName { get; set; } = string.Empty;

    // ===== Thời gian =====
    public DateTime BookingDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string TimeRangeText => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";

    // ===== Tiền =====
    public decimal TotalAmount { get; set; }

    // ===== Trạng thái =====
    public int Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = string.Empty;

    public int PaymentStatus { get; set; }
    public string PaymentStatusText { get; set; } = string.Empty;
    public string PaymentMethodText { get; set; } = string.Empty;

    // ===== Thông tin khác =====
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledBy { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? PaymentDeadline { get; set; }
    public DateTime CreatedAt { get; set; }

    // ===== Thông tin khách hàng (cho Owner/Admin xem) =====
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;

    // ===== Quyền thao tác =====
    public bool CanCancel { get; set; }
    public string? CancelDisabledReason { get; set; }
    public bool CanReview { get; set; }

    /// <summary>Thời gian còn lại để thanh toán (phút).</summary>
    public int? MinutesLeftToPay =>
        PaymentDeadline.HasValue && PaymentDeadline.Value > SportBooking.Helpers.ScheduleGuard.Now
            ? (int)Math.Ceiling((PaymentDeadline.Value - SportBooking.Helpers.ScheduleGuard.Now).TotalMinutes)
            : null;

    // ===== Giao dịch thanh toán =====
    public List<PaymentInfoViewModel> Payments { get; set; } = new();

    // ===== Đánh giá (nếu đã đánh giá) =====
    public ReviewInfoViewModel? Review { get; set; }
}

/// <summary>
/// Thông tin một giao dịch thanh toán.
/// </summary>
public class PaymentInfoViewModel
{
    public int Id { get; set; }
    public string TransactionCode { get; set; } = string.Empty;
    public string MethodText { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = string.Empty;
    public string? BankCode { get; set; }
    public string? ResponseMessage { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Thông tin đánh giá của booking.
/// </summary>
public class ReviewInfoViewModel
{
    public int Id { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string? OwnerReply { get; set; }
    public DateTime? OwnerReplyAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
