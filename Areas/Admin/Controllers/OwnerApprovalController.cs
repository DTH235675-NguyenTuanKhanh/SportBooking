using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SportBooking.Constants;
using SportBooking.Models.Entities;
using SportBooking.Models.ViewModels.Admin;
using SportBooking.Services.Interfaces;

namespace SportBooking.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppConstants.Roles.Admin)]
public sealed class OwnerApprovalController : Controller
{
    private readonly IAdminService _adminService;
    private readonly UserManager<ApplicationUser> _userManager;

    public OwnerApprovalController(
        IAdminService adminService,
        UserManager<ApplicationUser> userManager)
    {
        _adminService = adminService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] OwnerApprovalListViewModel filter,
        CancellationToken cancellationToken)
    {
        return View(await _adminService.GetOwnerApprovalsAsync(filter, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Detail(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var owner = await _adminService.GetOwnerApprovalDetailAsync(id, cancellationToken);
        return owner is null ? NotFound() : View(owner);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Decide(
        OwnerApprovalDecisionViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData[AppConstants.TempDataKeys.Error] = "Dữ liệu xử lý yêu cầu không hợp lệ.";
            return RedirectToAction(nameof(Detail), new { id = model.UserId });
        }

        var adminId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(adminId))
        {
            return Challenge();
        }

        var result = await _adminService.DecideOwnerApprovalAsync(adminId, model, cancellationToken);
        TempData[result.Success ? AppConstants.TempDataKeys.Success : AppConstants.TempDataKeys.Error] = result.Message;
        return RedirectToAction(nameof(Detail), new { id = model.UserId });
    }
}
