using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SportBooking.Constants;
using SportBooking.Models.Entities;
using SportBooking.Services.Interfaces;

namespace SportBooking.Areas.Owner.Controllers;

[Area("Owner")]
[Authorize(Roles = AppConstants.Roles.Owner)]
public sealed class ReviewsController(UserManager<ApplicationUser> users, IOwnerReviewService reviews) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1)
    {
        var owner = await ApprovedOwnerAsync();
        return owner is null ? Forbid() : View(await reviews.ListAsync(owner.Id, page));
    }

    [HttpGet]
    public async Task<IActionResult> Reply(int id)
    {
        var owner = await ApprovedOwnerAsync();
        if (owner is null) return Forbid();
        var review = await reviews.FindAsync(owner.Id, id);
        return review is { Visible: true } ? View(review) : NotFound();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int id, string? reply)
    {
        var owner = await ApprovedOwnerAsync();
        if (owner is null) return Forbid();
        var review = await reviews.FindAsync(owner.Id, id);
        if (review is not { Visible: true }) return NotFound();
        var result = await reviews.ReplyAsync(owner.Id, id, reply);
        if (result.Success)
        {
            TempData[AppConstants.TempDataKeys.Success] = result.Message;
            return RedirectToAction(nameof(Index));
        }
        ModelState.AddModelError(nameof(reply), result.Message);
        ViewData["DraftReply"] = reply;
        return View(review);
    }

    private async Task<ApplicationUser?> ApprovedOwnerAsync() => await users.GetUserAsync(User)
        is { IsActive: true, ApprovalStatus: OwnerApprovalStatus.Approved } owner ? owner : null;
}
