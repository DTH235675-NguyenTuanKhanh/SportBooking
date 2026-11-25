// File: Helpers/VietnamPhoneAttribute.cs
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SportBooking.Helpers;

/// <summary>
/// Validation attribute kiểm tra số điện thoại Việt Nam hợp lệ.
/// Chấp nhận các định dạng: 0912345678, 0912345678 (10 số), +84912345678
/// Đầu số hợp lệ: 03, 05, 07, 08, 09 (đầu di động hiện hành).
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class VietnamPhoneAttribute : ValidationAttribute
{
    // Đầu số Việt Nam: 03x, 05x, 07x, 08x, 09x + 8 chữ số, hoặc có tiền tố +84
    private static readonly Regex PhoneRegex = new(
        @"^(0|\+84)(3|5|7|8|9)\d{8}$",
        RegexOptions.Compiled);

    /// <summary>
    /// Cho phép chuỗi rỗng/null khi thuộc tính là tùy chọn.
    /// Mặc định false: bắt buộc phải có số điện thoại.
    /// </summary>
    public bool AllowEmpty { get; set; } = false;

    public VietnamPhoneAttribute()
    {
        ErrorMessage = "Số điện thoại Việt Nam không hợp lệ (ví dụ: 0912345678).";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // Nếu cho phép rỗng và giá trị rỗng thì bỏ qua kiểm tra.
        if (AllowEmpty && string.IsNullOrWhiteSpace(value as string))
        {
            return ValidationResult.Success;
        }

        if (value is not string phone || string.IsNullOrWhiteSpace(phone))
        {
            return new ValidationResult(ErrorMessage, new[] { validationContext.MemberName ?? string.Empty });
        }

        // Loại bỏ khoảng trắng, dấu gạch ngang, dấu chấm để chuẩn hóa trước khi kiểm tra.
        var normalized = phone.Trim()
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Replace(".", string.Empty);

        if (!PhoneRegex.IsMatch(normalized))
        {
            return new ValidationResult(ErrorMessage, new[] { validationContext.MemberName ?? string.Empty });
        }

        return ValidationResult.Success;
    }
}