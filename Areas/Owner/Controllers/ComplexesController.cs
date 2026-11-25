using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportBooking.Constants;
using SportBooking.Data;
using SportBooking.Models.Entities;
using SportBooking.Models.ViewModels.Owner;

namespace SportBooking.Areas.Owner.Controllers;

[Area("Owner")]
[Authorize(Roles = AppConstants.Roles.Owner)]
public sealed class ComplexesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : Controller
{
    private async Task<ApplicationUser?> ApprovedOwner()
    {
        var owner = await userManager.GetUserAsync(User);
        return owner is { IsActive: true, ApprovalStatus: OwnerApprovalStatus.Approved } ? owner : null;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? keyword, bool? isActive, int page = 1)
    {
        var owner = await ApprovedOwner();
        if (owner is null) return Forbid();
        var query = context.SportComplexes.AsNoTracking().Where(c => c.OwnerId == owner.Id);
        keyword = keyword?.Trim();
        if (!string.IsNullOrEmpty(keyword)) query = query.Where(c => c.Name.Contains(keyword) || c.Address.Contains(keyword)
            || c.Province.Contains(keyword) || c.District.Contains(keyword));
        if (isActive.HasValue) query = query.Where(c => c.IsActive == isActive.Value);
        var count = await query.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(count / 10d)));
        return View(new ComplexListViewModel
        {
            Keyword = keyword, IsActive = isActive, Page = page, TotalCount = count,
            Items = await query.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id)
                .Skip((page - 1) * 10).Take(10).Select(c => new ComplexListItem
                {
                    Id = c.Id, Name = c.Name, Address = c.Address, Province = c.Province, District = c.District,
                    PhoneNumber = c.PhoneNumber, Email = c.Email, IsActive = c.IsActive, CourtCount = c.Courts.Count()
                }).ToListAsync()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (await ApprovedOwner() is null) return Forbid();
        return View("Edit", new ComplexEditViewModel());
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var owner = await ApprovedOwner();
        if (owner is null) return Forbid();
        var item = await context.SportComplexes.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id && c.OwnerId == owner.Id);
        if (item is null) return NotFound();
        return View(new ComplexEditViewModel
        {
            Name = item.Name, Address = item.Address, Province = item.Province, District = item.District,
            Description = item.Description, ImageUrl = item.ImageUrl, PhoneNumber = item.PhoneNumber,
            Email = item.Email, OpeningTime = item.OpeningTime, ClosingTime = item.ClosingTime, IsActive = item.IsActive
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create(ComplexEditViewModel model) => Save(null, model);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(int id, ComplexEditViewModel model) => Save(id, model);

    private async Task<IActionResult> Save(int? id, ComplexEditViewModel model)
    {
        var owner = await ApprovedOwner();
        if (owner is null) return Forbid();
        model.Name = model.Name?.Trim() ?? string.Empty;
        model.Address = model.Address?.Trim() ?? string.Empty;
        model.Province = model.Province?.Trim() ?? string.Empty;
        model.District = model.District?.Trim() ?? string.Empty;
        model.Description = model.Description?.Trim();
        model.ImageUrl = model.ImageUrl?.Trim();
        model.PhoneNumber = model.PhoneNumber?.Trim();
        model.Email = model.Email?.Trim();
        foreach (var (field, value, limit) in new[]
        {
            (nameof(model.Name), model.Name, 200), (nameof(model.Address), model.Address, 300),
            (nameof(model.Province), model.Province, 100), (nameof(model.District), model.District, 100),
            (nameof(model.Description), model.Description, 1000), (nameof(model.ImageUrl), model.ImageUrl, 500),
            (nameof(model.PhoneNumber), model.PhoneNumber, 20), (nameof(model.Email), model.Email, 256)
        })
        {
            if (value?.Length > limit) ModelState.AddModelError(field, $"Tối đa {limit} ký tự.");
        }
        foreach (var (field, value) in new[] { (nameof(model.Name), model.Name),
            (nameof(model.Address), model.Address), (nameof(model.Province), model.Province),
            (nameof(model.District), model.District) })
        {
            if (string.IsNullOrWhiteSpace(value)) ModelState.AddModelError(field, "Thông tin này không được để trống.");
        }
        return await context.Database.CreateExecutionStrategy().ExecuteAsync<IActionResult>(async () =>
        {
            context.ChangeTracker.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync();
            if (id.HasValue) await SportBooking.Helpers.ScheduleGuard.LockAsync(context, id.Value);
            var item = id.HasValue
                ? await context.SportComplexes.SingleOrDefaultAsync(c => c.Id == id && c.OwnerId == owner.Id)
                : new SportComplex { OwnerId = owner.Id };
            if (item is null) return NotFound();
            if (id.HasValue && ModelState.IsValid && model.OpeningTime.HasValue
                && (item.OpeningTime != model.OpeningTime || item.ClosingTime != model.ClosingTime))
            {
                var start = model.OpeningTime.Value; var end = model.ClosingTime!.Value;
                if (await context.TimeSlots.AnyAsync(t => t.Court.SportComplexId == id.Value && t.IsActive
                    && (t.StartTime < start || t.EndTime > end)))
                    ModelState.AddModelError(nameof(model.OpeningTime), "Giờ mới không bao phủ các khung đang bán. Hãy chỉnh/ngừng bán khung trước.");
                var now = SportBooking.Helpers.ScheduleGuard.Now;
                if (await context.Bookings.AnyAsync(b => b.Court.SportComplexId == id.Value && b.EndDateTime > now
                    && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed)
                    && (b.StartDateTime.TimeOfDay < start || b.EndDateTime.TimeOfDay > end)))
                    ModelState.AddModelError(nameof(model.OpeningTime), "Giờ mới ảnh hưởng booking chưa kết thúc.");
            }
            if (!ModelState.IsValid) return View("Edit", model);
            item.Name = model.Name.Trim();
            item.Address = model.Address.Trim();
            item.Province = model.Province.Trim();
            item.District = model.District.Trim();
            item.Description = model.Description?.Trim();
            item.ImageUrl = model.ImageUrl?.Trim();
            item.PhoneNumber = model.PhoneNumber?.Trim();
            item.Email = model.Email?.Trim();
            item.OpeningTime = model.OpeningTime;
            item.ClosingTime = model.ClosingTime;
            item.IsActive = model.IsActive;
            if (id.HasValue) item.UpdatedAt = DateTime.UtcNow;
            else context.SportComplexes.Add(item);
            try { await context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { return NotFound(); }
            await transaction.CommitAsync();
            TempData[AppConstants.TempDataKeys.Success] = id.HasValue ? "Đã cập nhật cụm sân." : "Đã thêm cụm sân.";
            return RedirectToAction(nameof(Index));
        });
    }
}
