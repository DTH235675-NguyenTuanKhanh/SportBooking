// File: Controllers/CourtController.cs
using Microsoft.AspNetCore.Mvc;
using SportBooking.Models.ViewModels.Court;
using SportBooking.Services.Interfaces;

namespace SportBooking.Controllers;

/// <summary>
/// Controller xử lý tìm kiếm sân và xem chi tiết sân.
/// Không yêu cầu đăng nhập (công khai cho tất cả mọi người).
/// </summary>
public class CourtController : Controller
{
    private readonly ICourtService _courtService;
    private readonly ILogger<CourtController> _logger;

    public CourtController(ICourtService courtService, ILogger<CourtController> logger)
    {
        _courtService = courtService;
        _logger = logger;
    }

    // =========================================================================
    // TÌM KIẾM SÂN
    // =========================================================================
    /// <summary>
    /// GET: /Court/Search
    /// Trang tìm kiếm sân với bộ lọc, sắp xếp và phân trang.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] CourtSearchViewModel filter)
    {
        try
        {
            var result = await _courtService.SearchCourtsAsync(filter);
            return View(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tìm kiếm sân.");
            TempData["Error"] = "Có lỗi xảy ra khi tìm kiếm sân. Vui lòng thử lại sau.";
            return View(new CourtSearchViewModel());
        }
    }

    // =========================================================================
    // CHI TIẾT SÂN
    // =========================================================================
    /// <summary>
    /// GET: /Court/Detail/5
    /// Trang chi tiết sân: thông tin sân, bảng giá, gallery, đánh giá.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Detail(int id, int reviewPage = 1, string? returnUrl = null)
    {
        ViewData["SearchReturnUrl"] = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action(nameof(Search));
        try
        {
            var detail = await _courtService.GetCourtDetailAsync(id, reviewPage, 5);

            if (detail is null)
            {
                TempData["Error"] = "Không tìm thấy sân.";
                return RedirectToAction(nameof(Search));
            }

            return View(detail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xem chi tiết sân {CourtId}.", id);
            TempData["Error"] = "Có lỗi xảy ra khi tải thông tin sân. Vui lòng thử lại sau.";
            return RedirectToAction(nameof(Search));
        }
    }

    // =========================================================================
    // API: LẤY DANH SÁCH QUẬN/HUYỆN THEO TỈNH (AJAX)
    // =========================================================================
    /// <summary>
    /// GET: /Court/GetDistricts?province=...
    /// API phụ trợ cho dropdown quận/huyện phụ thuộc tỉnh.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDistricts(string province)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(province))
            {
                return Json(new List<string>());
            }

            var districts = await _courtService.GetDistrictsAsync(province);
            return Json(districts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách quận/huyện cho tỉnh {Province}.", province);
            return Json(new List<string>());
        }
    }
}
