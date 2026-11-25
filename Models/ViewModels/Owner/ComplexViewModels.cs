using System.ComponentModel.DataAnnotations;

namespace SportBooking.Models.ViewModels.Owner;

public class ComplexEditViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Vui lòng nhập tên cụm sân."), StringLength(200)]
    [Display(Name = "Tên cụm sân")]
    public string Name { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng nhập địa chỉ."), StringLength(300)]
    [Display(Name = "Địa chỉ chi tiết")]
    public string Address { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng nhập tỉnh/thành phố."), StringLength(100)]
    [Display(Name = "Tỉnh / Thành phố")]
    public string Province { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng nhập quận/huyện."), StringLength(100)]
    [Display(Name = "Quận / Huyện")]
    public string District { get; set; } = string.Empty;
    [StringLength(1000), Display(Name = "Mô tả, tiện ích")]
    public string? Description { get; set; }
    [StringLength(500), Display(Name = "URL ảnh đại diện")]
    public string? ImageUrl { get; set; }
    [StringLength(20), Phone(ErrorMessage = "Số điện thoại không hợp lệ."), Display(Name = "Điện thoại liên hệ")]
    public string? PhoneNumber { get; set; }
    [StringLength(256), EmailAddress(ErrorMessage = "Email không hợp lệ."), Display(Name = "Email liên hệ")]
    public string? Email { get; set; }
    [DataType(DataType.Time), Display(Name = "Giờ mở cửa")]
    public TimeSpan? OpeningTime { get; set; }
    [DataType(DataType.Time), Display(Name = "Giờ đóng cửa")]
    public TimeSpan? ClosingTime { get; set; }
    [Display(Name = "Đang hoạt động")]
    public bool IsActive { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(ImageUrl)
            && (!Uri.TryCreate(ImageUrl.Trim(), UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)))
            yield return new ValidationResult("Ảnh phải là URL http hoặc https hợp lệ.", new[] { nameof(ImageUrl) });
        if (OpeningTime.HasValue != ClosingTime.HasValue)
            yield return new ValidationResult("Nhập đủ giờ mở và đóng cửa, hoặc để trống cả hai.", new[] { nameof(ClosingTime) });
        if (OpeningTime.HasValue && (OpeningTime < TimeSpan.Zero || OpeningTime >= TimeSpan.FromDays(1)))
            yield return new ValidationResult("Giờ mở cửa không hợp lệ.", new[] { nameof(OpeningTime) });
        if (ClosingTime.HasValue && (ClosingTime < TimeSpan.Zero || ClosingTime >= TimeSpan.FromDays(1)))
            yield return new ValidationResult("Giờ đóng cửa không hợp lệ.", new[] { nameof(ClosingTime) });
        if (OpeningTime.HasValue && ClosingTime.HasValue && OpeningTime >= ClosingTime)
            yield return new ValidationResult("Giờ đóng cửa phải sau giờ mở cửa trong cùng ngày.", new[] { nameof(ClosingTime) });
        if (OpeningTime.HasValue && OpeningTime.Value.Ticks % TimeSpan.TicksPerMinute != 0)
            yield return new ValidationResult("Giờ mở cửa phải chính xác đến phút.", new[] { nameof(OpeningTime) });
        if (ClosingTime.HasValue && ClosingTime.Value.Ticks % TimeSpan.TicksPerMinute != 0)
            yield return new ValidationResult("Giờ đóng cửa phải chính xác đến phút.", new[] { nameof(ClosingTime) });
    }
}

public class ComplexListViewModel
{
    public List<ComplexListItem> Items { get; set; } = new();
    public string? Keyword { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / 10d);
}

public class ComplexListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public int CourtCount { get; set; }
}
