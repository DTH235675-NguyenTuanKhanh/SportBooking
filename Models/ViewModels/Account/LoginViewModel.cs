// File: Models/ViewModels/Account/LoginViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace SportBooking.Models.ViewModels.Account;

/// <summary>
/// ViewModel cho form đăng nhập.
/// </summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "Email là bắt buộc.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ghi nhớ đăng nhập")]
    public bool RememberMe { get; set; } = false;

    /// <summary>URL quay lại sau khi đăng nhập thành công (nếu có).</summary>
    public string? ReturnUrl { get; set; }
}