// File: Models/ViewModels/Account/ForgotPasswordViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace SportBooking.Models.ViewModels.Account;

/// <summary>
/// ViewModel cho chức năng quên mật khẩu (bước 1: nhập email để nhận link đặt lại).
/// </summary>
public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Email là bắt buộc.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [Display(Name = "Email đã đăng ký")]
    public string Email { get; set; } = string.Empty;
}