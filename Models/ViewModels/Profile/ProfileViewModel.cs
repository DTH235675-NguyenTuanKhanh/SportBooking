// File: Models/ViewModels/Profile/ProfileViewModel.cs
using System.ComponentModel.DataAnnotations;
using SportBooking.Helpers;

namespace SportBooking.Models.ViewModels.Profile;

/// <summary>
/// ViewModel cho chức năng cập nhật hồ sơ người dùng.
/// Hỗ trợ cả Customer và Owner (Owner có thêm thông tin doanh nghiệp).
/// </summary>
public class ProfileViewModel
{
    /// <summary>ID người dùng (không hiển thị, dùng để xác định user khi submit).</summary>
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
    [StringLength(150, ErrorMessage = "Họ và tên tối đa 150 ký tự.")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
    [VietnamPhone]
    [Display(Name = "Số điện thoại")]
    public string PhoneNumber { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Ngày sinh")]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(300, ErrorMessage = "Địa chỉ tối đa 300 ký tự.")]
    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }

    /// <summary>URL ảnh đại diện hiện tại (nếu có).</summary>
    [Display(Name = "Ảnh đại diện")]
    public string? AvatarUrl { get; set; }

    /// <summary>File ảnh mới để upload (tùy chọn).</summary>
    [Display(Name = "Thay đổi ảnh đại diện")]
    public IFormFile? AvatarFile { get; set; }

    // ===== Thông tin bổ sung cho Owner =====

    /// <summary>Vai trò hiện tại của user (để hiển thị các trường phù hợp).</summary>
    public string CurrentRole { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Tên doanh nghiệp tối đa 200 ký tự.")]
    [Display(Name = "Tên doanh nghiệp / Cơ sở")]
    public string? BusinessName { get; set; }

    [VietnamPhone(AllowEmpty = true)]
    [Display(Name = "Số điện thoại kinh doanh")]
    public string? BusinessPhone { get; set; }

    /// <summary>Email (chỉ hiển thị, không cho phép chỉnh sửa sau khi đã tạo tài khoản).</summary>
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Trạng thái tài khoản Owner (chỉ hiển thị, không chỉnh sửa).</summary>
    public bool IsApproved { get; set; }
}