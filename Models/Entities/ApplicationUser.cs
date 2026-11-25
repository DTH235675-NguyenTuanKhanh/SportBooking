// File: Models/Entities/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;

namespace SportBooking.Models.Entities;

/// <summary>
/// Người dùng hệ thống, kế thừa IdentityUser để hỗ trợ Identity.
/// Các tác nhân: Customer, Owner, Admin đều dùng chung bảng này, phân biệt qua Role.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Họ và tên đầy đủ.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Ngày sinh (tùy chọn).</summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>Địa chỉ liên hệ.</summary>
    public string? Address { get; set; }

    /// <summary>Đường dẫn ảnh đại diện (lưu trong wwwroot/uploads/avatars).</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>Tài khoản có bị khóa không (false = bị khóa).</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Dành cho Owner: Tên doanh nghiệp / cơ sở kinh doanh.
    /// Customer/Admin để null.
    /// </summary>
    public string? BusinessName { get; set; }

    /// <summary>
    /// Dành cho Owner: Số điện thoại kinh doanh (có thể khác SĐT cá nhân).
    /// </summary>
    public string? BusinessPhone { get; set; }

    /// <summary>
    /// Dành cho Owner: Trạng thái phê duyệt tài khoản.
    /// Owner mới đăng ký = Pending, phải chờ Admin duyệt mới vào được khu vực Owner.
    /// </summary>
    public OwnerApprovalStatus ApprovalStatus { get; set; } = OwnerApprovalStatus.NotApplicable;

    /// <summary>Thời điểm tạo tài khoản.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Thời điểm cập nhật cuối.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Thời điểm Admin xử lý yêu cầu đăng ký Owner gần nhất.</summary>
    public DateTime? ApprovalReviewedAt { get; set; }

    /// <summary>ID của Admin đã xử lý yêu cầu đăng ký Owner gần nhất.</summary>
    public string? ApprovalReviewedById { get; set; }

    /// <summary>Lý do từ chối đăng ký Owner (nếu có).</summary>
    public string? ApprovalRejectionReason { get; set; }

    // ========== Navigation Properties ==========

    /// <summary>Danh sách cụm sân mà chủ sân này sở hữu (chỉ Owner mới có).</summary>
    public ICollection<SportComplex> SportComplexes { get; set; } = new List<SportComplex>();

    /// <summary>Danh sách booking khách hàng này đã đặt (chỉ Customer mới có nhiều).</summary>
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    /// <summary>Danh sách đánh giá khách này viết.</summary>
    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    /// <summary>Danh sách thông báo (tùy chọn, nếu có chức năng thông báo).</summary>
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
