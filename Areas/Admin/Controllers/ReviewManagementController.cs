using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportBooking.Constants;
using SportBooking.Models.ViewModels.Admin;
using SportBooking.Services.Interfaces;

namespace SportBooking.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppConstants.Roles.Admin)]
public sealed class ReviewManagementController : Controller
{
    private readonly IAdminService _adminService;

    public ReviewManagementController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] AdminReviewListViewModel filter,
        CancellationToken cancellationToken)
    {
        return View(await _adminService.GetReviewsAsync(filter, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetVisibility(
        int reviewId,
        bool isVisible,
        AdminReviewListViewModel filter,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.SetReviewVisibilityAsync(
            reviewId, isVisible, cancellationToken);
        TempData[result.Success ? AppConstants.TempDataKeys.Success : AppConstants.TempDataKeys.Error] = result.Message;
        return RedirectToAction(nameof(Index), new
        {
            filter.CourtId,
            filter.Rating,
            filter.VisibilityFilter,
            filter.Keyword,
            filter.Page,
            filter.PageSize
        });
    }
}
