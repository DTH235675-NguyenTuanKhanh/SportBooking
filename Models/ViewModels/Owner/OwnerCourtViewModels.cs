using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SportBooking.Models.Entities;

namespace SportBooking.Models.ViewModels.Owner;

public class OwnerCourtEditViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên sân."), StringLength(150)]
    [Display(Name = "Tên sân")]
    public string Name { get; set; } = string.Empty;
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn loại sân.")]
    [Display(Name = "Loại sân")]
    public int SportTypeId { get; set; }
    [StringLength(1000), Display(Name = "Mô tả sân")]
    public string? Description { get; set; }
    [StringLength(500), Display(Name = "URL ảnh sân")]
    [RegularExpression(@"(https?://[^\s]+|/uploads/[^\s\\]+)", ErrorMessage = "Nhập URL http/https hoặc đường dẫn /uploads/ hợp lệ.")]
    public string? ImageUrl { get; set; }
    [EnumDataType(typeof(CourtStatus)), Display(Name = "Trạng thái")]
    public CourtStatus Status { get; set; } = CourtStatus.Active;
    [BindNever] public int ComplexId { get; set; }
    [BindNever] public string ComplexName { get; set; } = string.Empty;
    [BindNever] public List<OwnerSportTypeOption> SportTypes { get; set; } = new();
}

public class OwnerSportTypeOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class OwnerCourtListViewModel
{
    public int ComplexId { get; set; }
    public string ComplexName { get; set; } = string.Empty;
    public bool ComplexIsActive { get; set; }
    public string? Keyword { get; set; }
    public CourtStatus? Status { get; set; }
    public List<OwnerCourtListItem> Items { get; set; } = new();
    public int Page { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / 10d);
    public static string StatusText(CourtStatus status) => status switch
    {
        CourtStatus.Active => "Hoạt động", CourtStatus.Maintenance => "Bảo trì", _ => "Ngừng hoạt động"
    };
}

public class OwnerCourtListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SportTypeName { get; set; } = string.Empty;
    public CourtStatus Status { get; set; }
    public int TimeSlotCount { get; set; }
}
