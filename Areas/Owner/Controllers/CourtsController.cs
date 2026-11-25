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
public sealed class CourtsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : Controller
{
    private async Task<ApplicationUser?> ApprovedOwner()
    {
        var owner = await userManager.GetUserAsync(User);
        return owner is { IsActive: true, ApprovalStatus: OwnerApprovalStatus.Approved } ? owner : null;
    }

    private Task<SportComplex?> FindComplex(int complexId, string ownerId) => context.SportComplexes
        .AsNoTracking().SingleOrDefaultAsync(c => c.Id == complexId && c.OwnerId == ownerId);

    [HttpGet]
    public async Task<IActionResult> Index(int complexId, string? keyword, CourtStatus? status, int page = 1)
    {
        var owner = await ApprovedOwner();
        if (owner is null) return Forbid();
        var complex = await FindComplex(complexId, owner.Id);
        if (complex is null) return NotFound();
        if (status.HasValue && !Enum.IsDefined(status.Value)) status = null;
        var query = context.Courts.AsNoTracking().Where(c => c.SportComplexId == complex.Id && c.SportComplex.OwnerId == owner.Id);
        keyword = keyword?.Trim();
        if (!string.IsNullOrEmpty(keyword)) query = query.Where(c => c.Name.Contains(keyword) || c.SportType.Name.Contains(keyword));
        if (status.HasValue) query = query.Where(c => c.Status == status.Value);
        var count = await query.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(count / 10d)));
        return View(new OwnerCourtListViewModel
        {
            ComplexId = complex.Id, ComplexName = complex.Name, ComplexIsActive = complex.IsActive,
            Keyword = keyword, Status = status, Page = page, TotalCount = count,
            Items = await query.OrderBy(c => c.Name).ThenBy(c => c.Id).Skip((page - 1) * 10).Take(10)
                .Select(c => new OwnerCourtListItem { Id = c.Id, Name = c.Name, SportTypeName = c.SportType.Name,
                    Status = c.Status, TimeSlotCount = c.TimeSlots.Count(t => t.IsActive) }).ToListAsync()
        });
    }

    private async Task Populate(OwnerCourtEditViewModel model, SportComplex complex, int? currentTypeId = null)
    {
        model.ComplexId = complex.Id;
        model.ComplexName = complex.Name;
        model.SportTypes = await context.SportTypes.AsNoTracking()
            .Where(t => t.IsActive || t.Id == currentTypeId).OrderBy(t => t.DisplayOrder).ThenBy(t => t.Name)
            .Select(t => new OwnerSportTypeOption { Id = t.Id, Name = t.Name, IsActive = t.IsActive }).ToListAsync();
    }

    [HttpGet]
    public async Task<IActionResult> Create(int complexId)
    {
        var owner = await ApprovedOwner();
        if (owner is null) return Forbid();
        var complex = await FindComplex(complexId, owner.Id);
        if (complex is null) return NotFound();
        var model = new OwnerCourtEditViewModel();
        await Populate(model, complex);
        return View("Edit", model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int complexId, int id)
    {
        var owner = await ApprovedOwner();
        if (owner is null) return Forbid();
        var complex = await FindComplex(complexId, owner.Id);
        if (complex is null) return NotFound();
        var court = await context.Courts.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id
            && c.SportComplexId == complex.Id && c.SportComplex.OwnerId == owner.Id);
        if (court is null) return NotFound();
        var model = new OwnerCourtEditViewModel { Name = court.Name, SportTypeId = court.SportTypeId,
            Description = court.Description, ImageUrl = court.ImageUrl, Status = court.Status };
        await Populate(model, complex, court.SportTypeId);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create(int complexId, OwnerCourtEditViewModel model) => Save(complexId, null, model);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(int complexId, int id, OwnerCourtEditViewModel model) => Save(complexId, id, model);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int complexId, int id)
    {
        var owner = await ApprovedOwner();
        if (owner is null) return Forbid();
        var complex = await FindComplex(complexId, owner.Id);
        if (complex is null) return NotFound();

        return await context.Database.CreateExecutionStrategy().ExecuteAsync<IActionResult>(async () =>
        {
            context.ChangeTracker.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync();
            await SportBooking.Helpers.ScheduleGuard.LockAsync(context, complex.Id);
            var court = await context.Courts.IgnoreQueryFilters().SingleOrDefaultAsync(c => c.Id == id
                && c.SportComplexId == complex.Id && c.SportComplex.OwnerId == owner.Id);
            if (court is null) return NotFound();
            if (court.IsDeleted)
            {
                TempData[AppConstants.TempDataKeys.Info] = "Sân đã được xóa trước đó.";
                return RedirectToAction(nameof(Index), new { complexId });
            }

            var now = SportBooking.Helpers.ScheduleGuard.Now;
            if (await context.Bookings.IgnoreQueryFilters().AnyAsync(b => b.CourtId == court.Id
                && b.EndDateTime > now && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed)))
            {
                TempData[AppConstants.TempDataKeys.Error] = "Sân còn booking chờ hoặc đã xác nhận chưa kết thúc. Hãy xử lý lịch đặt trước khi xóa.";
                return RedirectToAction(nameof(Index), new { complexId });
            }

            court.IsDeleted = true;
            court.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData[AppConstants.TempDataKeys.Success] = "Đã xóa sân. Lịch sử đặt sân vẫn được giữ lại.";
            return RedirectToAction(nameof(Index), new { complexId });
        });
    }

    private async Task<IActionResult> Save(int complexId, int? id, OwnerCourtEditViewModel model)
    {
        var owner = await ApprovedOwner();
        if (owner is null) return Forbid();
        var complex = await FindComplex(complexId, owner.Id);
        if (complex is null) return NotFound();
        return await context.Database.CreateExecutionStrategy().ExecuteAsync<IActionResult>(async () =>
        {
            context.ChangeTracker.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync();
            await SportBooking.Helpers.ScheduleGuard.LockAsync(context, complex.Id);
            var court = id.HasValue ? await context.Courts.SingleOrDefaultAsync(c => c.Id == id.Value
                && c.SportComplexId == complex.Id && c.SportComplex.OwnerId == owner.Id) : new Court { SportComplexId = complex.Id };
            if (court is null) return NotFound();
            var type = await context.SportTypes.AsNoTracking().SingleOrDefaultAsync(t => t.Id == model.SportTypeId);
            if (type is null || (!type.IsActive && (!id.HasValue || court.SportTypeId != type.Id)))
                ModelState.AddModelError(nameof(model.SportTypeId), "Chọn loại sân đang hoạt động.");
            if (!string.IsNullOrWhiteSpace(model.ImageUrl) && !model.ImageUrl.StartsWith("/uploads/", StringComparison.Ordinal)
                && (!Uri.TryCreate(model.ImageUrl, UriKind.Absolute, out var uri)
                    || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)))
                ModelState.AddModelError(nameof(model.ImageUrl), "URL ảnh không hợp lệ.");
            if (!ModelState.IsValid)
            {
                await Populate(model, complex, id.HasValue ? court.SportTypeId : null);
                return View("Edit", model);
            }
            court.Name = model.Name.Trim();
            court.SportTypeId = model.SportTypeId;
            court.Description = model.Description?.Trim();
            court.ImageUrl = model.ImageUrl?.Trim();
            court.Status = model.Status;
            if (id.HasValue) court.UpdatedAt = DateTime.UtcNow;
            else context.Courts.Add(court);
            try { await context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { return NotFound(); }
            await transaction.CommitAsync();
            TempData[AppConstants.TempDataKeys.Success] = id.HasValue ? "Đã cập nhật sân." : "Đã thêm sân.";
            return id.HasValue ? RedirectToAction(nameof(Index), new { complexId = complex.Id })
                : RedirectToAction("Index", "TimeSlots", new { courtId = court.Id });
        });
    }
}
