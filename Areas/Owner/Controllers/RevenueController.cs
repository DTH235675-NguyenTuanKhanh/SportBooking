using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SportBooking.Constants;
using SportBooking.Models.Entities;
using SportBooking.Models.ViewModels.Owner;
using SportBooking.Services.Interfaces;

namespace SportBooking.Areas.Owner.Controllers;

[Area("Owner")]
[Authorize(Roles = AppConstants.Roles.Owner)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class RevenueController(UserManager<ApplicationUser> users, IOwnerRevenueService revenue) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] OwnerRevenueFilter filter, CancellationToken cancellationToken)
    {
        var owner = await users.GetUserAsync(User);
        if (owner is not { IsActive: true, ApprovalStatus: OwnerApprovalStatus.Approved }) return Forbid();
        return View(await revenue.ReadAsync(owner.Id, filter, ModelState.IsValid, cancellationToken));
    }
}
