// File: Models/Entities/Payment.cs
namespace SportBooking.Models.Entities;

/// <summary>
/// Giao dịch thanh toán gắn với một booking.
/// Một booking có thể có nhiều Payment (ví dụ: thanh toán thất bại rồi thử lại).
/// </summary>
public class Payment
{
    /// <summary>Khóa chính.</summary>
    public int Id { get; set; }

    /// <summary>Khóa ngoại tới booking.</summary>
    public int BookingId { get; set; }

    /// <summary>
    /// Mã giao dịch nội bộ gửi sang cổng thanh toán (vnp_TxnRef / MoMo orderId).
    /// Phải là duy nhất để đối soát.
    /// </summary>
    public string TransactionCode { get; set; } = string.Empty;

    /// <summary>Phương thức thanh toán của giao dịch này.</summary>
    public PaymentMethod Method { get; set; }

    /// <summary>Số tiền giao dịch.</summary>
    public decimal Amount { get; set; }

    /// <summary>Trạng thái giao dịch.</summary>
    public PaymentStatus Status { get; set; } = PaymentStatus.Unpaid;

    /// <summary>Mã giao dịch trả về từ cổng thanh toán (vnp_TransactionNo / MoMo transId).</summary>
    public string? GatewayTransactionId { get; set; }

    /// <summary>Mã ngân hàng / ví khách dùng để thanh toán.</summary>
    public string? BankCode { get; set; }

    /// <summary>Mã phản hồi từ cổng (vnp_ResponseCode / MoMo resultCode).</summary>
    public string? ResponseCode { get; set; }

    /// <summary>Thông điệp phản hồi từ cổng (đã giải mã / mô tả).</summary>
    public string? ResponseMessage { get; set; }

    /// <summary>Dữ liệu thô (raw query string / JSON) nhận từ cổng, phục vụ đối soát và debug.</summary>
    public string? RawData { get; set; }

    /// <summary>Thời điểm cổng thanh toán xác nhận giao dịch thành công.</summary>
    public DateTime? PaidAt { get; set; }

    public DateTime? ManualRefundedAt { get; set; }
    [System.ComponentModel.DataAnnotations.MaxLength(450)]
    public string? ManualRefundedById { get; set; }
    [System.ComponentModel.DataAnnotations.MaxLength(300)]
    public string? ManualRefundNote { get; set; }

    /// <summary>Thời điểm tạo giao dịch.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Thời điểm cập nhật cuối.</summary>
    public DateTime? UpdatedAt { get; set; }

    // ========== Navigation Properties ==========

    /// <summary>Booking mà giao dịch này thanh toán.</summary>
    public Booking Booking { get; set; } = null!;
}
