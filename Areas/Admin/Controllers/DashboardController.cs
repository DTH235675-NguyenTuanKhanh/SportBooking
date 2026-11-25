using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportBooking.Constants;
using SportBooking.Services.Interfaces;

namespace SportBooking.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppConstants.Roles.Admin)]
public sealed class DashboardController : Controller
{
    private readonly IAdminService _adminService;

    public DashboardController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await _adminService.GetDashboardAsync(cancellationToken));
    }
}
