// File: Helpers/DisplayHelper.cs
using System.Globalization;

namespace SportBooking.Helpers;

/// <summary>
/// Các hàm định dạng dùng trong view: tiền VNĐ, ngày dd/MM/yyyy,
/// thứ trong tuần tiếng Việt và thời gian tương đối.
/// Tập trung một chỗ để các trang hiển thị nhất quán, không lặp format string.
/// </summary>
public static class DisplayHelper
{
    private static readonly CultureInfo ViCulture = new("vi-VN");

    private static readonly string[] WeekdayNames =
    {
        "Chủ nhật", "Thứ hai", "Thứ ba", "Thứ tư", "Thứ năm", "Thứ sáu", "Thứ bảy"
    };

    /// <summary>Định dạng tiền: 120.000 ₫.</summary>
    public static string ToVnd(this decimal value)
        => value.ToString("#,##0", ViCulture) + " ₫";

    /// <summary>Định dạng ngày: 25/10/2026.</summary>
    public static string ToDate(this DateTime date) => date.ToString("dd/MM/yyyy", ViCulture);

    /// <summary>Định dạng ngày giờ: 18:30 25/10/2026.</summary>
    public static string ToDateTime(this DateTime date) => date.ToString("HH:mm dd/MM/yyyy", ViCulture);

    public static string ToDate(this DateTime? date)
        => date.HasValue ? date.Value.ToDate() : "Chưa cập nhật";

    public static string ToDateTime(this DateTime? date)
        => date.HasValue ? date.Value.ToDateTime() : "Chưa cập nhật";

    /// <summary>Thứ trong tuần bằng tiếng Việt (Chủ nhật, Thứ hai, ...).</summary>
    public static string WeekdayName(this DateTime date) => WeekdayNames[(int)date.DayOfWeek];

    /// <summary>
    /// Thời gian tương đối ngắn gọn cho danh sách đánh giá:
    /// "vừa xong", "5 phút trước", "3 giờ trước", "2 ngày trước", sau đó tới ngày cụ thể.
    /// </summary>
    public static string ToRelativeTime(this DateTime date)
    {
        var diff = DateTime.Now - date;

        if (diff.TotalMinutes < 1)
        {
            return "vừa xong";
        }

        if (diff.TotalHours < 1)
        {
            return $"{(int)diff.TotalMinutes} phút trước";
        }

        if (diff.TotalDays < 1)
        {
            return $"{(int)diff.TotalHours} giờ trước";
        }

        return diff.TotalDays <= 30
            ? $"{(int)diff.TotalDays} ngày trước"
            : date.ToDate();
    }

    /// <summary>Chuỗi khung giờ "hh:mm - hh:mm" từ hai giá trị TimeSpan.</summary>
    public static string ToTimeRange(this TimeSpan start, TimeSpan end)
        => $"{start:hh\\:mm} - {end:hh\\:mm}";
}
