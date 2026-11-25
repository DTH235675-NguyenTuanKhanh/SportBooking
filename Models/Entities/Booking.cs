// File: Models/Entities/Booking.cs
namespace SportBooking.Models.Entities;

/// <summary>
/// Lượt đặt sân của khách hàng.
/// Mỗi bản ghi tương ứng 1 khung giờ của 1 sân trong 1 ngày cụ thể.
/// Nếu khách chọn nhiều khung giờ liên tiếp, hệ thống tạo nhiều bản ghi
/// cùng nhóm bởi <see cref="BookingGroupCode"/>.
/// </summary>
public class Booking
{
    /// <summary>Khóa chính.</summary>
    public int Id { get; set; }

    /// <summary>Mã đặt sân duy nhất hiển thị cho khách (ví dụ: SB20260101-0001).</summary>
    public string BookingCode { get; set; } = string.Empty;

    /// <summary>
    /// Mã nhóm đặt sân: nhiều khung giờ liên tiếp trong cùng 1 lần đặt
    /// sẽ chia sẻ cùng BookingGroupCode để gom nhóm thanh toán / hiển thị.
    /// </summary>
    public string BookingGroupCode { get; set; } = string.Empty;
    public string? BankTransferGroupCode { get; set; }
    public BankTransfer? BankTransfer { get; set; }

    /// <summary>Khóa ngoại tới khách hàng đặt sân.</summary>
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>Khóa ngoại tới sân được đặt.</summary>
    public int CourtId { get; set; }

    /// <summary>Khóa ngoại tới khung giờ được đặt.</summary>
    public int TimeSlotId { get; set; }

    /// <summary>Ngày đặt sân (chỉ phần ngày, không có giờ).</summary>
    public DateOnly BookingDate { get; set; }

    /// <summary>Thời điểm bắt đầu thực tế = BookingDate + TimeSlot.StartTime (lưu sẵn để truy vấn nhanh).</summary>
    public DateTime StartDateTime { get; set; }

    /// <summary>Thời điểm kết thúc thực tế = BookingDate + TimeSlot.EndTime (lưu sẵn để truy vấn nhanh).</summary>
    public DateTime EndDateTime { get; set; }

    /// <summary>Tổng tiền của bản ghi này (đã snapshot giá tại thời điểm đặt).</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Trạng thái booking.</summary>
    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    /// <summary>Trạng thái thanh toán.</summary>
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

    /// <summary>Phương thức thanh toán khách chọn.</summary>
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.AtCounter;

    /// <summary>Ghi chú của khách khi đặt sân.</summary>
    public string? Note { get; set; }

    /// <summary>Lý do hủy (bắt buộc khi khách hủy đặt sân).</summary>
    public string? CancellationReason { get; set; }

    /// <summary>Thời điểm hủy / hết hạn.</summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>Người hủy: "Customer", "Owner", "System".</summary>
    public string? CancelledBy { get; set; }

    /// <summary>Thời điểm chủ sân xác nhận thanh toán tại quầy.</summary>
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>Thời điểm hoàn thành (khách đã sử dụng sân).</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Hạn thanh toán online: = CreatedAt + 15 phút.
    /// BackgroundService dùng cột này để tự hủy booking quá hạn.
    /// </summary>
    public DateTime? PaymentDeadline { get; set; }

    /// <summary>Ngày tạo booking.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Ngày cập nhật cuối.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Khóa lạc quan (optimistic concurrency) chống 2 người đặt trùng.
    /// EF Core tự tăng giá trị mỗi lần UPDATE; nếu xung đột sẽ ném DbUpdateConcurrencyException.
    /// </summary>
    public byte[]? RowVersion { get; set; }

    // ========== Navigation Properties ==========

    /// <summary>Khách hàng đặt sân.</summary>
    public ApplicationUser Customer { get; set; } = null!;

    /// <summary>Sân được đặt.</summary>
    public Court Court { get; set; } = null!;

    /// <summary>Khung giờ được đặt.</summary>
    public TimeSlot TimeSlot { get; set; } = null!;

    /// <summary>Danh sách giao dịch thanh toán gắn với booking này.</summary>
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    /// <summary>Đánh giá của khách cho booking này (1-1, có thể null nếu chưa đánh giá).</summary>
    public Review? Review { get; set; }
}
