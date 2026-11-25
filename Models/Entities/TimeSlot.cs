// File: Models/Entities/TimeSlot.cs
namespace SportBooking.Models.Entities;

/// <summary>
/// Khung giờ đặt sân kèm giá theo loại ngày.
/// Ví dụ: Sân số 1, 18:00-19:00, ngày thường 200.000đ, cuối tuần 250.000đ, ngày lễ 300.000đ.
/// Một sân có nhiều khung giờ; khung giờ không được chồng lấn trong cùng sân.
/// </summary>
public class TimeSlot
{
    /// <summary>Khóa chính.</summary>
    public int Id { get; set; }

    /// <summary>Khóa ngoại tới sân.</summary>
    public int CourtId { get; set; }

    /// <summary>Giờ bắt đầu (ví dụ: 18:00).</summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>Giờ kết thúc (ví dụ: 19:00).</summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>
    /// Thời lượng khung giờ tính bằng phút.
    /// Được tính tự động từ StartTime và EndTime khi lưu.
    /// </summary>
    public int DurationMinutes { get; set; }

    /// <summary>Giá áp dụng cho ngày thường (Thứ 2 - Thứ 6).</summary>
    public decimal PriceWeekday { get; set; }

    /// <summary>Giá áp dụng cho cuối tuần (Thứ 7, Chủ nhật).</summary>
    public decimal PriceWeekend { get; set; }

    /// <summary>Giá áp dụng cho ngày lễ, Tết. Nếu = 0 sẽ fallback về giá cuối tuần.</summary>
    public decimal PriceHoliday { get; set; }

    /// <summary>Khung giờ có đang áp dụng không (cho phép tạm ngưng mà không xóa).</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Ngày tạo.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Ngày cập nhật cuối.</summary>
    public DateTime? UpdatedAt { get; set; }

    // ========== Navigation Properties ==========

    /// <summary>Sân sở hữu khung giờ này.</summary>
    public Court Court { get; set; } = null!;

    /// <summary>Danh sách booking dùng khung giờ này.</summary>
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    /// <summary>
    /// Lấy giá theo loại ngày. Quy tắc fallback:
    /// Holiday (nếu > 0) -> Weekend -> Weekday.
    /// </summary>
    public decimal GetPriceByDayType(DayType dayType) => dayType switch
    {
        DayType.Holiday when PriceHoliday > 0 => PriceHoliday,
        DayType.Holiday => PriceWeekend,
        DayType.Weekend => PriceWeekend,
        _ => PriceWeekday
    };
}