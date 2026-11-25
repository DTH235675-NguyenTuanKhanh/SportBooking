using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SportBooking.Models.Entities;

namespace SportBooking.Models.ViewModels.Owner;

public class OwnerTimeSlotEditViewModel : IValidatableObject
{
    [Required, DataType(DataType.Time), Display(Name = "Giờ bắt đầu")]
    public TimeSpan? StartTime { get; set; }
    [Required, DataType(DataType.Time), Display(Name = "Giờ kết thúc")]
    public TimeSpan? EndTime { get; set; }
    [Range(typeof(decimal), "1", "999999999999"), Display(Name = "Giá ngày thường (VNĐ/khung)")]
    public decimal PriceWeekday { get; set; }
    [Range(typeof(decimal), "1", "999999999999"), Display(Name = "Giá cuối tuần (VNĐ/khung)")]
    public decimal PriceWeekend { get; set; }
    [Display(Name = "Đang bán")]
    public bool IsActive { get; set; } = true;
    [BindNever] public int CourtId { get; set; }
    [BindNever] public int ComplexId { get; set; }
    [BindNever] public string CourtName { get; set; } = string.Empty;
    [BindNever] public string ComplexName { get; set; } = string.Empty;
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartTime.HasValue && EndTime.HasValue && (StartTime < TimeSpan.Zero
            || EndTime >= TimeSpan.FromDays(1) || StartTime >= EndTime
            || StartTime.Value.Ticks % TimeSpan.TicksPerMinute != 0 || EndTime.Value.Ticks % TimeSpan.TicksPerMinute != 0))
            yield return new ValidationResult("Giờ phải cùng ngày, chính xác đến phút; giờ kết thúc sau giờ bắt đầu và trước 24:00.", new[] { nameof(EndTime) });
        if (PriceWeekday != decimal.Truncate(PriceWeekday) || PriceWeekend != decimal.Truncate(PriceWeekend))
            yield return new ValidationResult("Giá phải là số nguyên VNĐ.", new[] { nameof(PriceWeekday), nameof(PriceWeekend) });
    }
}

public class OwnerTimeSlotListViewModel
{
    public int CourtId { get; set; }
    public int ComplexId { get; set; }
    public string CourtName { get; set; } = string.Empty;
    public string ComplexName { get; set; } = string.Empty;
    public List<TimeSlot> Items { get; set; } = new();
}
