using System.ComponentModel.DataAnnotations;
using SportBooking.Models.Entities;

namespace SportBooking.Models.ViewModels.Owner;

/// <summary>
/// Bộ lọc và danh sách lịch đặt sân dành cho Chủ sân.
/// </summary>
public class OwnerBookingListViewModel
{
    public List<OwnerBookingItemViewModel> Bookings { get; set; } = new();

    [Display(Name = "Trạng thái")]
    public string? StatusFilter { get; set; }
    public bool TransferReview { get; set; }

    [Display(Name = "Từ khóa")]
    public string? Keyword { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Từ ngày")]
    public DateTime? FromDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Đến ngày")]
    public DateTime? ToDate { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}

public class OwnerBookingItemViewModel
{
    public int Id { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string BookingGroupCode { get; set; } = string.Empty;
    public string? BankTransferGroupCode { get; set; }
    public TransferState? TransferState { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CourtName { get; set; } = string.Empty;
    public string ComplexName { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public decimal TotalAmount { get; set; }
    public BookingStatus Status { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }

    public string StatusText => Services.Implementations.BookingService.MapBookingStatusText(Status);
    public string StatusBadgeClass => Services.Implementations.BookingService.MapBookingStatusBadge(Status);
    public string PaymentStatusText => Services.Implementations.BookingService.MapPaymentStatusText(PaymentStatus);
    public string PaymentMethodText => Services.Implementations.BookingService.MapPaymentMethod(PaymentMethod);

    public bool CanConfirmCounterPayment => Status == BookingStatus.Pending
        && PaymentStatus == PaymentStatus.Unpaid
        && PaymentMethod == PaymentMethod.AtCounter
        && StartDateTime > SportBooking.Helpers.ScheduleGuard.Now;

    public bool CanComplete => Status == BookingStatus.Confirmed
        && PaymentStatus == PaymentStatus.Paid
        && EndDateTime <= SportBooking.Helpers.ScheduleGuard.Now;

    public bool CanConfirmCounterRefund => Status == BookingStatus.Cancelled
        && PaymentStatus == PaymentStatus.RefundPending
        && PaymentMethod == PaymentMethod.AtCounter;

    public bool CanMarkNoShow => Status == BookingStatus.Confirmed
        && PaymentStatus == PaymentStatus.Paid
        && StartDateTime <= SportBooking.Helpers.ScheduleGuard.Now;
}
