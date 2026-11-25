// File: Models/ViewModels/Account/ResetPasswordViewModel.cs
using System.ComponentModel.DataAnnotations;
using SportBooking.Helpers;

namespace SportBooking.Models.ViewModels.Account;

/// <summary>
/// ViewModel cho bước 2 của quên mật khẩu: đặt lại mật khẩu mới bằng token nhận qua email.
/// </summary>
public class ResetPasswordViewModel
{
    [Required]
    public string Email { get; set; } = string.Empty;

    /// <summary>Token đặt lại mật khẩu do Identity sinh ra và gửi qua email.</summary>
    [Required]
    public string Token { get; set; } = string.Empty;

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