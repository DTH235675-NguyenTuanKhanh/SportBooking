// File: Models/ViewModels/Shared/PaginationViewModel.cs
namespace SportBooking.Models.ViewModels.Shared;

/// <summary>
/// ViewModel cho partial phân trang dùng chung (_Pagination).
/// Tự sinh link trang theo bộ lọc hiện tại để giữ nguyên điều kiện lọc khi chuyển trang.
/// </summary>
public class PaginationViewModel
{
    /// <summary>Trang hiện tại (bắt đầu từ 1).</summary>
    public int Page { get; set; } = 1;

    /// <summary>Tổng số trang.</summary>
    public int TotalPages { get; set; }

    /// <summary>Tổng số bản ghi (hiển thị "Kết quả 1 - 12 / 57").</summary>
    public int TotalResults { get; set; }

    /// <summary>Số bản ghi mỗi trang.</summary>
    public int PageSize { get; set; } = 12;

    /// <summary>Controller đích của link phân trang.</summary>
    public string Controller { get; set; } = "Court";

    /// <summary>Action đích của link phân trang.</summary>
    public string Action { get; set; } = "Search";

    /// <summary>Area đích (nếu có).</summary>
    public string? Area { get; set; }

    /// <summary>
    /// Các tham số bộ lọc đi kèm (không gồm Page). Giá trị null/rỗng sẽ được bỏ qua.
    /// </summary>
    public Dictionary<string, string?> RouteValues { get; set; } = new();

    /// <summary>Nhãn số ít của bản ghi, ví dụ "sân", "lịch đặt".</summary>
    public string ItemLabel { get; set; } = "kết quả";

    /// <summary>Không phân trang khi chỉ có 1 trang.</summary>
    public bool ShouldRender => TotalPages > 1;

    /// <summary>Số bản ghi đầu tiên đang hiển thị trên trang hiện tại.</summary>
    public int FromItem => TotalResults == 0 ? 0 : (Page - 1) * PageSize + 1;

    /// <summary>Số bản ghi cuối cùng đang hiển thị trên trang hiện tại.</summary>
    public int ToItem => Math.Min(Page * PageSize, TotalResults);

    /// <summary>Trang đầu của dải nút trang hiển thị quanh trang hiện tại.</summary>
    public int WindowStart => Math.Max(1, Page - 2);

    /// <summary>Trang cuối của dải nút trang hiển thị quanh trang hiện tại.</summary>
    public int WindowEnd => Math.Min(TotalPages, WindowStart + 4);

    /// <summary>
    /// Sinh link tới một trang cụ thể, giữ nguyên toàn bộ bộ lọc hiện tại.
    /// </summary>
    public string BuildUrl(int targetPage)
    {
        var query = new List<string>();

        foreach (var (key, value) in RouteValues)
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(key, "page", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            query.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
        }

        if (targetPage > 1)
        {
            query.Add($"Page={targetPage}");
        }

        var path = string.IsNullOrWhiteSpace(Area)
            ? $"/{Controller}/{Action}"
            : $"/{Area}/{Controller}/{Action}";
        return query.Count == 0 ? path : $"{path}?{string.Join("&", query)}";
    }
}
