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
public sealed class UserManagementController : Controller
{
    private readonly IAdminService _adminService;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserManagementController(
        IAdminService adminService,
        UserManager<ApplicationUser> userManager)
    {
        _adminService = adminService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] AdminUserListViewModel filter,
        CancellationToken cancellationToken)
    {
        return View(await _adminService.GetUsersAsync(filter, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Detail(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var user = await _adminService.GetUserDetailAsync(id, cancellationToken);
        return user is null ? NotFound() : View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(
        string userId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var adminId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(adminId))
        {
            return Challenge();
        }

        var result = await _adminService.SetUserActiveAsync(
            adminId, userId, isActive, cancellationToken);
        TempData[result.Success ? AppConstants.TempDataKeys.Success : AppConstants.TempDataKeys.Error] = result.Message;
        return RedirectToAction(nameof(Detail), new { id = userId });
    }
}
