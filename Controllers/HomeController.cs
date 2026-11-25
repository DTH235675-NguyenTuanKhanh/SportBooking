// File: Controllers/HomeController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportBooking.Models.ViewModels.Home;
using SportBooking.Services.Interfaces;

namespace SportBooking.Controllers;

/// <summary>
/// Controller cho trang chủ và các trang thông tin chung.
/// </summary>
public class HomeController : Controller
{
    private readonly ICourtService _courtService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ICourtService courtService, ILogger<HomeController> logger)
    {
        _courtService = courtService;
        _logger = logger;
    }

    /// <summary>
    /// GET: /
    /// Trang chủ với hero search, loại sân, sân nổi bật và đánh giá nổi bật.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        try
        {
            var model = await _courtService.GetHomeDataAsync();
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tải dữ liệu trang chủ.");
            // Trả về ViewModel rỗng để trang vẫn hiển thị được, tránh lỗi trắng trang.
            return View(new HomeViewModel());
        }
    }

    /// <summary>Trang giới thiệu / điều khoản sử dụng.</summary>
    public IActionResult Privacy()
    {
        return View();
    }

    /// <summary>Trang báo lỗi hệ thống (500).</summary>
    [AllowAnonymous]
    public IActionResult ServerError()
    {
        return View("~/Views/Shared/ServerError.cshtml");
    }

    /// <summary>
    /// Trang báo lỗi theo mã trạng thái (được cấu hình bởi UseStatusCodePagesWithReExecute).
    /// </summary>
    [AllowAnonymous]
    [Route("Home/Error/{statusCode:int}")]
    public IActionResult Error(int statusCode)
    {
        // 404 dùng trang riêng, các mã khác dùng trang lỗi chung.
        if (statusCode == 404)
        {
            return View("~/Views/Shared/NotFound.cshtml");
        }

        return View("~/Views/Shared/ServerError.cshtml");
    }
}
