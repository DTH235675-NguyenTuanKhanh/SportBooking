using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportBooking.Constants;
using SportBooking.Data;
using SportBooking.Helpers;
using SportBooking.Models.Entities;
using SportBooking.Models.ViewModels.Owner;
using SportBooking.Services.Interfaces;

namespace SportBooking.Areas.Owner.Controllers;

[Area("Owner")]
[Authorize(Roles = AppConstants.Roles.Owner)]
public sealed class DashboardController(ApplicationDbContext context,
    UserManager<ApplicationUser> userManager, IOwnerBookingService bookingService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var owner = await userManager.GetUserAsync(User);
        if (owner is not { IsActive: true, ApprovalStatus: OwnerApprovalStatus.Approved }) return Forbid();
        var now = ScheduleGuard.Now;
        var today = now.Date;
        return View(new OwnerDashboardViewModel
        {
            OwnerName = owner.FullName,
            Today = today,
            ComplexCount = await context.SportComplexes.CountAsync(c => c.OwnerId == owner.Id),
            CourtCount = await context.Courts.CountAsync(c => c.SportComplex.OwnerId == owner.Id),
            PendingCount = await context.Bookings.CountAsync(b => b.Court.SportComplex.OwnerId == owner.Id
                && b.Status == BookingStatus.Pending && b.PaymentMethod == PaymentMethod.AtCounter
                && b.PaymentStatus == PaymentStatus.Unpaid && b.StartDateTime > now),
            TodayBookings = await bookingService.GetBookingsAsync(owner.Id,
                new OwnerBookingListViewModel { FromDate = today, ToDate = today, PageSize = 5 })
        });
    }
}
