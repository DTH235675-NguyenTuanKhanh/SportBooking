// File: Helpers/PasswordStrengthAttribute.cs
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SportBooking.Helpers;

/// <summary>
/// Validation attribute kiểm tra độ mạnh mật khẩu theo quy tắc nghiệp vụ:
/// - Tối thiểu 8 ký tự
/// - Có ít nhất 1 chữ hoa (A-Z)
/// - Có ít nhất 1 chữ thường (a-z)
/// - Có ít nhất 1 chữ số (0-9)
/// Dùng để validate phía server; phía client dùng thêm remote validation.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class PasswordStrengthAttribute : ValidationAttribute
{
    private static readonly Regex HasUpper = new("[A-Z]", RegexOptions.Compiled);
    private static readonly Regex HasLower = new("[a-z]", RegexOptions.Compiled);
    private static readonly Regex HasDigit = new("[0-9]", RegexOptions.Compiled);

    public int MinimumLength { get; set; } = 8;

    public PasswordStrengthAttribute()
    {
        ErrorMessage = "Mật khẩu phải có tối thiểu 8 ký tự, bao gồm chữ hoa, chữ thường và chữ số.";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string password || string.IsNullOrWhiteSpace(password))
        {
            return new ValidationResult("Mật khẩu là bắt buộc.",
                new[] { validationContext.MemberName ?? string.Empty });
        }

        if (password.Length < MinimumLength)
        {
            return new ValidationResult(
                $"Mật khẩu phải có tối thiểu {MinimumLength} ký tự.",
                new[] { validationContext.MemberName ?? string.Empty });
        }

        if (!HasUpper.IsMatch(password))
        {
            return new ValidationResult("Mật khẩu phải có ít nhất 1 chữ hoa.",
                new[] { validationContext.MemberName ?? string.Empty });
        }

        if (!HasLower.IsMatch(password))
        {
            return new ValidationResult("Mật khẩu phải có ít nhất 1 chữ thường.",
                new[] { validationContext.MemberName ?? string.Empty });
        }

        if (!HasDigit.IsMatch(password))
        {
            return new ValidationResult("Mật khẩu phải có ít nhất 1 chữ số.",
                new[] { validationContext.MemberName ?? string.Empty });
        }

        return ValidationResult.Success;
    }
}