// File: Models/Entities/Enums.cs
namespace SportBooking.Models.Entities;

/// <summary>
/// Trạng thái của một lượt đặt sân.
/// Luồng chính: Pending -> Confirmed -> Completed
/// Nhánh phụ   : Cancelled, Expired, NoShow
/// </summary>
public enum BookingStatus
{
    /// <summary>Chờ thanh toán (online) hoặc chờ xác nhận (tại quầy).</summary>
    Pending = 0,

    /// <summary>Đã xác nhận (đã thanh toán online hoặc chủ sân xác nhận thu tiền tại quầy).</summary>
    Confirmed = 1,

    /// <summary>Đã hoàn thành (khách đã sử dụng sân xong).</summary>
    Completed = 2,

    /// <summary>Khách chủ động hủy (có lý do).</summary>
    Cancelled = 3,

    /// <summary>Hết hạn: quá 15 phút chưa thanh toán online, hoặc quá giờ bắt đầu mà chưa xác nhận tại quầy.</summary>
    Expired = 4,

    /// <summary>Khách không đến sân (chủ sân đánh dấu vắng mặt).</summary>
    NoShow = 5
}

/// <summary>
/// Trạng thái thanh toán của booking.
/// </summary>
public enum PaymentStatus
{
    /// <summary>Chưa thanh toán.</summary>
    Unpaid = 0,

    /// <summary>Đã thanh toán.</summary>
    Paid = 1,

    /// <summary>Đã hoàn tiền.</summary>
    Refunded = 2,

    /// <summary>Thanh toán thất bại.</summary>
    Failed = 3,

    /// <summary>Đã hủy lượt đặt, đang chờ xử lý trả lại tiền.</summary>
    RefundPending = 4
}

/// <summary>
/// Phương thức thanh toán khách chọn khi đặt sân.
/// </summary>
public enum PaymentMethod
{
    /// <summary>Thanh toán tại quầy (trả tiền mặt/chuyển khoản khi đến sân).</summary>
    AtCounter = 0,

    /// <summary>Thanh toán online qua cổng VNPay.</summary>
    VnPay = 1,

    /// <summary>Thanh toán online qua cổng MoMo.</summary>
    MoMo = 2,

    /// <summary>Chuyển khoản trực tiếp theo ảnh QR của chủ sân.</summary>
    BankTransfer = 3
}

/// <summary>
/// Loại ngày dùng để áp giá khung giờ (ngày thường / cuối tuần / ngày lễ).
/// </summary>
public enum DayType
{
    /// <summary>Ngày thường (thứ 2 - thứ 6).</summary>
    Weekday = 0,

    /// <summary>Cuối tuần (thứ 7, chủ nhật).</summary>
    Weekend = 1,

    /// <summary>Ngày lễ, Tết (ưu tiên cao nhất khi tra giá).</summary>
    Holiday = 2
}

/// <summary>
/// Trạng thái hoạt động của sân con.
/// </summary>
public enum CourtStatus
{
    /// <summary>Sân đang hoạt động, có thể đặt.</summary>
    Active = 0,

    /// <summary>Sân đang bảo trì, không cho đặt.</summary>
    Maintenance = 1,

    /// <summary>Ngừng hoạt động / đã xóa mềm.</summary>
    Inactive = 2
}

/// <summary>
/// Trạng thái tài khoản chủ sân (chờ duyệt / đã duyệt / bị từ chối).
/// </summary>
public enum OwnerApprovalStatus
{
    /// <summary>Chưa gửi hoặc không áp dụng (tài khoản khách hàng/admin).</summary>
    NotApplicable = 0,

    /// <summary>Đang chờ Admin duyệt.</summary>
    Pending = 1,

    /// <summary>Admin đã duyệt, được vào khu vực Owner.</summary>
    Approved = 2,

    /// <summary>Admin từ chối đăng ký.</summary>
    Rejected = 3
}

/// <summary>
/// Trạng thái của khung giờ trong một ngày cụ thể (dùng để render lưới chọn giờ).
/// Đây là trạng thái tính toán (không lưu DB).
/// </summary>
public enum TimeSlotAvailability
{
    /// <summary>Khung giờ còn trống, có thể chọn.</summary>
    Available = 0,

    /// <summary>Đã có người đặt.</summary>
    Booked = 1,

    /// <summary>Sân bảo trì.</summary>
    Maintenance = 2,

    /// <summary>Khung giờ đã trôi qua so với hiện tại.</summary>
    Passed = 3
}
