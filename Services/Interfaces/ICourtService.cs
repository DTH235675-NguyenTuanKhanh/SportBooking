// File: Services/Interfaces/ICourtService.cs
using SportBooking.Models.ViewModels.Court;
using SportBooking.Models.ViewModels.Home;

namespace SportBooking.Services.Interfaces;

/// <summary>
/// Nghiệp vụ liên quan đến sân: tìm kiếm, xem chi tiết, lấy dữ liệu trang chủ.
/// </summary>
public interface ICourtService
{
    /// <summary>
    /// Tìm kiếm sân với bộ lọc, sắp xếp và phân trang.
    /// Chỉ trả về sân đang hoạt động (Status = Active) và chưa bị xóa mềm.
    /// </summary>
    Task<CourtSearchViewModel> SearchCourtsAsync(CourtSearchViewModel filter);

    /// <summary>
    /// Lấy thông tin chi tiết sân để hiển thị trang chi tiết.
    /// </summary>
    /// <param name="courtId">ID sân.</param>
    /// <param name="reviewPage">Trang đánh giá hiện tại.</param>
    /// <param name="reviewPageSize">Số đánh giá mỗi trang.</param>
    /// <returns>Null nếu không tìm thấy sân.</returns>
    Task<CourtDetailViewModel?> GetCourtDetailAsync(int courtId, int reviewPage = 1, int reviewPageSize = 5);

    /// <summary>
    /// Lấy dữ liệu cho trang chủ: loại sân, sân nổi bật, đánh giá nổi bật, thống kê.
    /// </summary>
    Task<HomeViewModel> GetHomeDataAsync();

    /// <summary>
    /// Lấy danh sách loại sân đang hoạt động (dùng cho dropdown).
    /// </summary>
    Task<List<SportTypeOption>> GetSportTypesAsync();

    /// <summary>
    /// Lấy danh sách tỉnh/thành phố có sân (dùng cho dropdown filter).
    /// </summary>
    Task<List<string>> GetProvincesAsync();

    /// <summary>
    /// Lấy danh sách quận/huyện theo tỉnh (dùng cho dropdown filter phụ thuộc).
    /// </summary>
    Task<List<string>> GetDistrictsAsync(string province);

    /// <summary>
    /// Lấy danh sách khung giờ phổ biến (dùng cho dropdown filter).
    /// </summary>
    Task<List<TimeSlotOption>> GetTimeSlotOptionsAsync();
}