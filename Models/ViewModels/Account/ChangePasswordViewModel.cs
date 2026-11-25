// File: Models/ViewModels/Account/ChangePasswordViewModel.cs
using System.ComponentModel.DataAnnotations;
using SportBooking.Helpers;

namespace SportBooking.Models.ViewModels.Account;

/// <summary>
/// ViewModel cho chức năng đổi mật khẩu khi đã đăng nhập.
/// </summary>
public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu hiện tại")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu mới là bắt buộc.")]
    [DataType(DataType.Password)]
    [PasswordStrength(MinimumLength = 8)]
    [Display(Name = "Mật khẩu mới")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    [Display(Name = "Xác nhận mật khẩu mới")]
    public string ConfirmPassword { get; set; } = string.Empty;
}