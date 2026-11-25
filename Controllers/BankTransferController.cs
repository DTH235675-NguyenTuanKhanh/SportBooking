using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SportBooking.Constants;
using SportBooking.Models.Entities;
using SportBooking.Services.Implementations;
using SportBooking.Services.Interfaces;

namespace SportBooking.Controllers;

[Authorize(Roles = AppConstants.Roles.Customer + "," + AppConstants.Roles.Owner)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class BankTransferController(BankTransferService transfers, UserManager<ApplicationUser> users) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Details(string code)
    {
        var scope = await GetScopeAsync();
        if (scope is null) return Forbid();
        var transfer = await transfers.GetAsync(code, scope.Value.OwnerId, scope.Value.CustomerId);
        if (transfer is null) return NotFound();
        ViewData["OwnerReview"] = scope.Value.OwnerId is not null;
        return View(transfer);
    }

    [HttpGet]
    public async Task<IActionResult> Qr(string code, bool download = false)
    {
        var scope = await GetScopeAsync();
        if (scope is null) return Forbid();
        var bytes = await transfers.GetQrAsync(code, scope.Value.OwnerId, scope.Value.CustomerId);
        if (bytes is null) return NotFound();
        return Image(bytes, download ? "sportbooking-qr.png" : null);
    }

    [HttpGet]
    public async Task<IActionResult> Receipt(string code, int reportId)
    {
        var scope = await GetScopeAsync();
        if (scope is null) return Forbid();
        var bytes = await transfers.GetReceiptAsync(code, reportId, scope.Value.OwnerId, scope.Value.CustomerId);
        return bytes is null ? NotFound() : Image(bytes);
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(3_000_000)]
    [Authorize(Roles = AppConstants.Roles.Customer)]
    public async Task<IActionResult> Report(string code, IFormFile? receipt)
    {
        var scope = await GetScopeAsync();
        if (scope?.CustomerId is not { } customerId) return Forbid();
        if (!await transfers.ExistsAsync(code, null, customerId)) return NotFound();
        return Complete(code, await transfers.ReportAsync(code, customerId, receipt));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = AppConstants.Roles.Owner)]
    public async Task<IActionResult> Review(string code, int reportId, string decision, string? note)
    {
        var scope = await GetScopeAsync();
        if (scope?.OwnerId is not { } ownerId) return Forbid();
        if (!await transfers.ExistsAsync(code, ownerId, null)) return NotFound();
        if (reportId < 1 || decision is not ("received" or "needs-info")) return BadRequest();
        return Complete(code, await transfers.ReviewAsync(code, reportId, ownerId, decision == "received", note));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = AppConstants.Roles.Owner)]
    public async Task<IActionResult> Refund(string code, int bookingId, string? note)
    {
        var scope = await GetScopeAsync();
        if (scope?.OwnerId is not { } ownerId) return Forbid();
        if (!await transfers.ExistsAsync(code, ownerId, null)) return NotFound();
        return Complete(code, await transfers.ConfirmRefundAsync(code, bookingId, ownerId, note));
    }

    private IActionResult Image(byte[] bytes, string? fileName = null)
    {
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(bytes, "image/png", fileName);
    }

    private IActionResult Complete(string code, ServiceResult result)
    {
        TempData[result.Success ? AppConstants.TempDataKeys.Success : AppConstants.TempDataKeys.Error] = result.Message;
        return RedirectToAction(nameof(Details), new { code });
    }

    private async Task<(string? OwnerId, string? CustomerId)?> GetScopeAsync()
    {
        var user = await users.GetUserAsync(User);
        if (user is not { IsActive: true }) return null;
        if (User.IsInRole(AppConstants.Roles.Owner))
            return user.ApprovalStatus == OwnerApprovalStatus.Approved ? (user.Id, null) : null;
        return User.IsInRole(AppConstants.Roles.Customer) ? (null, user.Id) : null;
    }
}
