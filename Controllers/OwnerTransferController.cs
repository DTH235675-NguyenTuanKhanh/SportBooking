using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SportBooking.Constants;
using SportBooking.Models.Entities;
using SportBooking.Models.ViewModels.Account;
using SportBooking.Services.Implementations;

namespace SportBooking.Controllers;

[Authorize(Roles = AppConstants.Roles.Owner)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class OwnerTransferController(TransferProfileService profiles, UserManager<ApplicationUser> users) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var owner = await ApprovedOwnerAsync();
        if (owner is null) return Forbid();
        return View(await profiles.GetAsync(owner.Id));
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(3_000_000)]
    public async Task<IActionResult> Edit(TransferProfileInput model)
    {
        var owner = await ApprovedOwnerAsync();
        if (owner is null) return Forbid();
        if (ModelState.IsValid)
        {
            var result = await profiles.SaveAsync(owner.Id, model);
            if (result.Success)
            {
                TempData[AppConstants.TempDataKeys.Success] = result.Message;
                return RedirectToAction(nameof(Edit));
            }
            if (result.Errors.Count == 0) ModelState.AddModelError(string.Empty, result.Message);
            foreach (var error in result.Errors)
                foreach (var message in error.Value)
                    ModelState.AddModelError(error.Key, message);
        }
        model.HasCurrentQr = (await profiles.GetAsync(owner.Id)).HasCurrentQr;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Qr()
    {
        var owner = await ApprovedOwnerAsync();
        if (owner is null) return Forbid();
        var bytes = await profiles.GetQrAsync(owner.Id);
        if (bytes is null) return NotFound();
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(bytes, "image/png");
    }

    private async Task<ApplicationUser?> ApprovedOwnerAsync() => await users.GetUserAsync(User)
        is { IsActive: true, ApprovalStatus: OwnerApprovalStatus.Approved } owner ? owner : null;
}
