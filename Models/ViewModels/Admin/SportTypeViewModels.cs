using System.ComponentModel.DataAnnotations;

namespace SportBooking.Models.ViewModels.Admin;

public class SportTypeEditViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên loại sân.")]
    [StringLength(100, ErrorMessage = "Tên tối đa 100 ký tự.")]
    [Display(Name = "Tên loại sân")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [StringLength(100)]
    [RegularExpression(@"bi-[a-z0-9-]+", ErrorMessage = "Nhập lớp Bootstrap Icons, ví dụ bi-trophy-fill.")]
    [Display(Name = "Biểu tượng")]
    public string? IconClass { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Thứ tự phải là số nguyên không âm.")]
    [Display(Name = "Thứ tự hiển thị")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Đang hoạt động")]
    public bool IsActive { get; set; } = true;
}

public class SportTypeListViewModel
{
    public List<SportTypeListItem> Items { get; set; } = new();
    public string? Keyword { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / 10d);
}

public class SportTypeListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconClass { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public int CourtCount { get; set; }
}
