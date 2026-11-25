using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SportBooking.Constants;
using SportBooking.Models.Entities;
using SportBooking.Models.ViewModels.Owner;
using SportBooking.Services.Interfaces;

namespace SportBooking.Areas.Owner.Controllers;

/// <summary>
/// Endpoint cho Chủ sân xác nhận tiền tại quầy và cập nhật kết quả sử dụng sân.
/// </summary>
[Area("Owner")]
[Authorize(Roles = AppConstants.Roles.Owner)]
public class BookingsController : Controller
{
    private readonly IOwnerBookingService _bookingService;
    private readonly UserManager<ApplicationUser> _userManager;

    public BookingsController(
        IOwnerBookingService bookingService,
        UserManager<ApplicationUser> userManager)
    {
        _bookingService = bookingService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] OwnerBookingListViewModel filter)
    {
        var owner = await GetApprovedOwnerAsync();
        if (owner is null)
        {
            return Forbid();
        }

        return View(await _bookingService.GetBookingsAsync(owner.Id, filter));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmCounterPayment(string bookingGroupCode, OwnerBookingListViewModel filter)
    {
        var owner = await GetApprovedOwnerAsync();
        if (owner is null)
        {
            return Forbid();
        }

        var result = await _bookingService.ConfirmCounterPaymentAsync(owner.Id, bookingGroupCode);
        TempData[result.Success ? AppConstants.TempDataKeys.Success : AppConstants.TempDataKeys.Error] = result.Message;
        return RedirectToAction(nameof(Index), FilterRouteValues(filter));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int bookingId, BookingStatus targetStatus, OwnerBookingListViewModel filter)
    {
        var owner = await GetApprovedOwnerAsync();
        if (owner is null)
        {
            return Forbid();
        }

        var result = await _bookingService.UpdateBookingStatusAsync(owner.Id, bookingId, targetStatus);
        TempData[result.Success ? AppConstants.TempDataKeys.Success : AppConstants.TempDataKeys.Error] = result.Message;
        return RedirectToAction(nameof(Index), FilterRouteValues(filter));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmCounterRefund(int bookingId, OwnerBookingListViewModel filter)
    {
        var owner = await GetApprovedOwnerAsync();
        if (owner is null) return Forbid();
        var result = await _bookingService.ConfirmCounterRefundAsync(owner.Id, bookingId);
        TempData[result.Success ? AppConstants.TempDataKeys.Success : AppConstants.TempDataKeys.Error] = result.Message;
        return RedirectToAction(nameof(Index), FilterRouteValues(filter));
    }

    private static object FilterRouteValues(OwnerBookingListViewModel filter) => new
    {
        filter.StatusFilter,
        filter.TransferReview,
        filter.Keyword,
        FromDate = filter.FromDate?.ToString("yyyy-MM-dd"),
        ToDate = filter.ToDate?.ToString("yyyy-MM-dd"),
        filter.Page,
        filter.PageSize
    };

    private async Task<ApplicationUser?> GetApprovedOwnerAsync()
    {
        var owner = await _userManager.GetUserAsync(User);
        return owner is { IsActive: true, ApprovalStatus: OwnerApprovalStatus.Approved } ? owner : null;
    }
}
