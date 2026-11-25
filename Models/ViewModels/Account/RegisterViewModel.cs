// File: Models/ViewModels/Account/RegisterViewModel.cs
using System.ComponentModel.DataAnnotations;
using SportBooking.Helpers;

namespace SportBooking.Models.ViewModels.Account;

/// <summary>
/// ViewModel cho form đăng ký.
/// Hỗ trợ 2 loại tài khoản: Khách hàng (Customer) và Chủ sân (Owner).
/// Khi chọn Owner, các trường doanh nghiệp trở thành bắt buộc (kiểm tra trong Validate).
/// </summary>
public class RegisterViewModel : IValidatableObject
{
    public TransferProfileInput Transfer { get; set; } = new();
    // ===== Thông tin cơ bản =====

    [Required(ErrorMessage = "Email là bắt buộc.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(256, ErrorMessage = "Email tối đa 256 ký tự.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
    [DataType(DataType.Password)]
    [PasswordStrength(MinimumLength = 8)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    [Display(Name = "Xác nhận mật khẩu")]
    public string ConfirmPassword { get; set; } = string.Empty;

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

    // ===== Loại tài khoản =====

    /// <summary>Loại tài khoản: "Customer" hoặc "Owner".</summary>
    [Required(ErrorMessage = "Vui lòng chọn loại tài khoản.")]
    [Display(Name = "Loại tài khoản")]
    public string AccountType { get; set; } = "Customer";

    // ===== Thông tin bổ sung cho Chủ sân =====

    [StringLength(200, ErrorMessage = "Tên doanh nghiệp tối đa 200 ký tự.")]
    [Display(Name = "Tên doanh nghiệp / Cơ sở")]
    public string? BusinessName { get; set; }

    [VietnamPhone(AllowEmpty = true)]
    [Display(Name = "Số điện thoại kinh doanh")]
    public string? BusinessPhone { get; set; }

    // ===== Điều khoản =====

    [Range(typeof(bool), "true", "true", ErrorMessage = "Bạn phải đồng ý với điều khoản sử dụng.")]
    [Display(Name = "Tôi đồng ý với điều khoản sử dụng")]
    public bool AgreeToTerms { get; set; }

    /// <summary>
    /// Validation liên trường: nếu đăng ký với vai trò Chủ sân
    /// thì Tên doanh nghiệp và SĐT kinh doanh là bắt buộc.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Chỉ chấp nhận 2 giá trị hợp lệ, chống over-posting đặt giá trị lạ.
        if (AccountType != "Customer" && AccountType != "Owner")
        {
            yield return new ValidationResult(
                "Loại tài khoản không hợp lệ.",
                new[] { nameof(AccountType) });
        }

        if (AccountType == "Owner")
        {
            if (string.IsNullOrWhiteSpace(BusinessName))
            {
                yield return new ValidationResult(
                    "Tên doanh nghiệp/cơ sở là bắt buộc đối với tài khoản Chủ sân.",
                    new[] { nameof(BusinessName) });
            }

            if (string.IsNullOrWhiteSpace(BusinessPhone))
            {
                yield return new ValidationResult(
                    "Số điện thoại kinh doanh là bắt buộc đối với tài khoản Chủ sân.",
                    new[] { nameof(BusinessPhone) });
            }
        }

        // Kiểm tra ngày sinh hợp lý: không ở tương lai, không quá 120 tuổi.
        if (DateOfBirth.HasValue)
        {
            if (DateOfBirth.Value.Date > DateTime.Today)
            {
                yield return new ValidationResult(
                    "Ngày sinh không thể ở tương lai.",
                    new[] { nameof(DateOfBirth) });
            }
            else if (DateOfBirth.Value.Date < DateTime.Today.AddYears(-120))
            {
                yield return new ValidationResult(
                    "Ngày sinh không hợp lệ.",
                    new[] { nameof(DateOfBirth) });
            }
        }
    }
}
