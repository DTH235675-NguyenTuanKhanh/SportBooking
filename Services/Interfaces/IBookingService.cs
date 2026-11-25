// File: Services/Interfaces/IBookingService.cs
using SportBooking.Models.ViewModels.Booking;

namespace SportBooking.Services.Interfaces;

/// <summary>
/// Nghiệp vụ đặt sân: tạo booking, kiểm tra khung giờ trống, hủy, đánh giá.
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Chuẩn bị dữ liệu cho trang đặt sân: thông tin sân + lưới khung giờ với trạng thái.
    /// </summary>
    /// <param name="courtId">ID sân.</param>
    /// <param name="bookingDate">Ngày muốn đặt (null = ngày mai).</param>
    Task<ServiceResult<BookingCreateViewModel>> PrepareBookingFormAsync(int courtId, DateTime? bookingDate = null);

    /// <summary>
    /// Lấy trạng thái khả dụng của tất cả khung giờ trong một ngày (dùng cho AJAX).
    /// </summary>
    /// <param name="courtId">ID sân.</param>
    /// <param name="bookingDate">Ngày cần kiểm tra.</param>
    Task<ServiceResult<List<TimeSlotAvailabilityViewModel>>> GetTimeSlotAvailabilityAsync(int courtId, DateTime bookingDate);

    /// <summary>
    /// Tạo booking mới.
    /// Kiểm tra chống đặt trùng ở tầng ứng dụng VÀ dựa vào unique index ở tầng database.
    /// </summary>
    /// <param name="customerId">ID khách hàng đang đặt.</param>
    /// <param name="model">Dữ liệu đặt sân.</param>
    /// <returns>Kết quả kèm mã booking nếu thành công.</returns>
    Task<ServiceResult<BookingCreatedResult>> CreateBookingAsync(string customerId, BookingCreateViewModel model);

    /// <summary>
    /// Lấy danh sách booking của khách hàng (có lọc và phân trang).
    /// </summary>
    Task<BookingListViewModel> GetCustomerBookingsAsync(string customerId, BookingListViewModel filter);

    /// <summary>
    /// Lấy chi tiết một booking.
    /// </summary>
    /// <param name="bookingId">ID booking.</param>
    /// <param name="userId">ID người dùng hiện tại (để kiểm tra quyền sở hữu).</param>
    /// <param name="isOwnerOrAdmin">Người dùng có phải Owner/Admin không.</param>
    Task<ServiceResult<BookingDetailViewModel>> GetBookingDetailAsync(int bookingId, string userId, bool isOwnerOrAdmin);

    /// <summary>
    /// Kiểm tra một booking có được phép hủy hay không (đúng trạng thái và còn đủ thời gian).
    /// </summary>
    Task<(bool CanCancel, string? Reason)> CanCancelBookingAsync(int bookingId, string customerId);

    /// <summary>
    /// Chuẩn bị dữ liệu cho form hủy booking.
    /// </summary>
    Task<ServiceResult<BookingCancelViewModel>> PrepareCancelFormAsync(int bookingId, string customerId);

    /// <summary>
    /// Hủy booking (bắt buộc có lý do).
    /// </summary>
    Task<ServiceResult> CancelBookingAsync(int bookingId, string customerId, string reason);

    /// <summary>
    /// Chuẩn bị dữ liệu cho form đánh giá.
    /// </summary>
    Task<ServiceResult<ReviewCreateViewModel>> PrepareReviewFormAsync(int bookingId, string customerId);

    /// <summary>
    /// Gửi đánh giá cho một booking đã hoàn thành.
    /// </summary>
    Task<ServiceResult> SubmitReviewAsync(string customerId, ReviewCreateViewModel model);
}

/// <summary>
/// Kết quả sau khi tạo booking thành công.
/// </summary>
public class BookingCreatedResult
{
    /// <summary>Mã nhóm booking (dùng để gom nhiều khung giờ).</summary>
    public string BookingGroupCode { get; set; } = string.Empty;

    /// <summary>Danh sách mã booking đã tạo.</summary>
    public List<string> BookingCodes { get; set; } = new();

    /// <summary>Tổng tiền.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Phương thức thanh toán.</summary>
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>ID của booking đầu tiên (để redirect tới trang chi tiết).</summary>
    public int FirstBookingId { get; set; }
}
