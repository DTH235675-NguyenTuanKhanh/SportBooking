using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SportBooking.Constants;
using SportBooking.Data;
using SportBooking.Helpers;
using SportBooking.Models.Entities;
using SportBooking.Models.ViewModels.Owner;

namespace SportBooking.Areas.Owner.Controllers;

[Area("Owner"), Authorize(Roles = AppConstants.Roles.Owner)]
public sealed class TimeSlotsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : Controller
{
    private async Task<ApplicationUser?> Owner()
    {
        var owner = await userManager.GetUserAsync(User);
        return owner is { IsActive: true, ApprovalStatus: OwnerApprovalStatus.Approved } ? owner : null;
    }
    private Task<Court?> Court(int courtId, string ownerId) => context.Courts.AsNoTracking()
        .Include(c => c.SportComplex).SingleOrDefaultAsync(c => c.Id == courtId && c.SportComplex.OwnerId == ownerId);
    private static void Populate(OwnerTimeSlotEditViewModel model, Court court)
    {
        model.CourtId = court.Id; model.ComplexId = court.SportComplexId;
        model.CourtName = court.Name; model.ComplexName = court.SportComplex.Name;
    }
    [HttpGet]
    public async Task<IActionResult> Index(int courtId)
    {
        var owner = await Owner();
        if (owner is null) return Forbid();
        var court = await Court(courtId, owner.Id);
        if (court is null) return NotFound();
        return View(new OwnerTimeSlotListViewModel { CourtId = court.Id, ComplexId = court.SportComplexId,
            CourtName = court.Name, ComplexName = court.SportComplex.Name,
            Items = await context.TimeSlots.AsNoTracking().Where(t => t.CourtId == court.Id).OrderBy(t => t.StartTime).ToListAsync() });
    }
    [HttpGet]
    public async Task<IActionResult> Create(int courtId)
    {
        var owner = await Owner(); if (owner is null) return Forbid();
        var court = await Court(courtId, owner.Id); if (court is null) return NotFound();
        var model = new OwnerTimeSlotEditViewModel(); Populate(model, court);
        return View("Edit", model);
    }
    [HttpGet]
    public async Task<IActionResult> Edit(int courtId, int id)
    {
        var owner = await Owner(); if (owner is null) return Forbid();
        var court = await Court(courtId, owner.Id); if (court is null) return NotFound();
        var slot = await context.TimeSlots.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id && t.CourtId == court.Id);
        if (slot is null) return NotFound();
        var model = new OwnerTimeSlotEditViewModel { StartTime = slot.StartTime, EndTime = slot.EndTime,
            PriceWeekday = slot.PriceWeekday, PriceWeekend = slot.PriceWeekend, IsActive = slot.IsActive };
        Populate(model, court); return View(model);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create(int courtId, OwnerTimeSlotEditViewModel model) => Save(courtId, null, model);
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(int courtId, int id, OwnerTimeSlotEditViewModel model) => Save(courtId, id, model);

    private async Task<IActionResult> Save(int courtId, int? id, OwnerTimeSlotEditViewModel model)
    {
        var owner = await Owner(); if (owner is null) return Forbid();
        var court = await Court(courtId, owner.Id); if (court is null) return NotFound();
        Populate(model, court);
        foreach (var field in new[] { nameof(model.PriceWeekday), nameof(model.PriceWeekend) })
        {
            var raw = Request.Form[field].ToString();
            if (string.IsNullOrEmpty(raw) || raw.Any(c => c < '0' || c > '9'))
                ModelState.AddModelError(field, "Nhập số nguyên VNĐ, không dùng dấu phân cách hoặc phần thập phân.");
        }
        return await context.Database.CreateExecutionStrategy().ExecuteAsync<IActionResult>(async () =>
        {
            context.ChangeTracker.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync();
            await ScheduleGuard.LockAsync(context, court.SportComplexId);
            court = await Court(courtId, owner.Id);
            if (court is null) return NotFound();
            var slot = id.HasValue ? await context.TimeSlots.SingleOrDefaultAsync(t => t.Id == id.Value && t.CourtId == courtId)
                : new TimeSlot { CourtId = courtId };
            if (slot is null) return NotFound();
            if (ModelState.IsValid)
            {
                var start = model.StartTime!.Value; var end = model.EndTime!.Value;
                if (court.SportComplex.OpeningTime.HasValue && (start < court.SportComplex.OpeningTime || end > court.SportComplex.ClosingTime))
                    ModelState.AddModelError(nameof(model.StartTime), "Khung phải nằm trong giờ mở cửa của cụm sân.");
                if (await context.TimeSlots.AnyAsync(t => t.CourtId == courtId && (!id.HasValue || t.Id != id.Value)
                    && t.StartTime == start))
                    ModelState.AddModelError(nameof(model.StartTime), "Đã có khung bắt đầu giờ này. Hãy sửa hoặc mở bán lại khung cũ.");
                if (model.IsActive && await context.TimeSlots.AnyAsync(t => t.CourtId == courtId && t.IsActive
                    && (!id.HasValue || t.Id != id.Value) && t.StartTime < end && t.EndTime > start))
                    ModelState.AddModelError(nameof(model.StartTime), "Khung giờ chồng lấn với khung đang bán.");
                var now = ScheduleGuard.Now;
                if (id.HasValue && (slot.StartTime != start || slot.EndTime != end)
                    && await context.Bookings.AnyAsync(b => b.TimeSlotId == id.Value && b.EndDateTime > now
                        && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed)))
                    ModelState.AddModelError(nameof(model.StartTime), "Khung có booking chưa kết thúc, không thể đổi giờ. Bạn vẫn có thể đổi giá hoặc ngừng bán.");
                // Old pending reservations on a retired/edited slot also block selling an overlapping window.
                if (model.IsActive && await context.Bookings.AnyAsync(b => b.CourtId == courtId && b.TimeSlotId != (id ?? 0)
                    && b.EndDateTime > now && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed)
                    && b.StartDateTime.TimeOfDay < end && b.EndDateTime.TimeOfDay > start))
                    ModelState.AddModelError(nameof(model.StartTime), "Giờ này còn booking trên khung khác. Chưa thể mở bán khung chồng lấn.");
            }
            if (!ModelState.IsValid) return View("Edit", model);
            slot.StartTime = model.StartTime!.Value; slot.EndTime = model.EndTime!.Value;
            slot.DurationMinutes = (int)(slot.EndTime - slot.StartTime).TotalMinutes;
            slot.PriceWeekday = model.PriceWeekday; slot.PriceWeekend = model.PriceWeekend; slot.IsActive = model.IsActive;
            if (id.HasValue) slot.UpdatedAt = DateTime.UtcNow; else context.TimeSlots.Add(slot);
            try { await context.SaveChangesAsync(); }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
                ModelState.AddModelError(nameof(model.StartTime), "Giờ bắt đầu đã tồn tại."); return View("Edit", model);
            }
            await transaction.CommitAsync();
            TempData[AppConstants.TempDataKeys.Success] = "Đã lưu khung giờ và giá.";
            return RedirectToAction(nameof(Index), new { courtId });
        });
    }
}
