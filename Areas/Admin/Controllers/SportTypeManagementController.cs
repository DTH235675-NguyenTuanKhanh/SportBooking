using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SportBooking.Constants;
using SportBooking.Data;
using SportBooking.Models.Entities;
using SportBooking.Models.ViewModels.Admin;

namespace SportBooking.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppConstants.Roles.Admin)]
public sealed class SportTypeManagementController(ApplicationDbContext context) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? keyword, bool? isActive, int page = 1)
    {
        keyword = keyword?.Trim();
        var query = context.SportTypes.AsNoTracking();
        if (!string.IsNullOrEmpty(keyword)) query = query.Where(t => t.Name.Contains(keyword));
        if (isActive.HasValue) query = query.Where(t => t.IsActive == isActive.Value);
        var count = await query.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(count / 10d)));
        return View(new SportTypeListViewModel
        {
            Keyword = keyword, IsActive = isActive, Page = page, TotalCount = count,
            Items = await query.OrderBy(t => t.DisplayOrder).ThenBy(t => t.Name).ThenBy(t => t.Id)
                .Skip((page - 1) * 10).Take(10).Select(t => new SportTypeListItem
                {
                    Id = t.Id, Name = t.Name, Description = t.Description, IconClass = t.IconClass,
                    DisplayOrder = t.DisplayOrder, IsActive = t.IsActive,
                    CourtCount = context.Courts.IgnoreQueryFilters().Count(c => c.SportTypeId == t.Id)
                }).ToListAsync()
        });
    }

    [HttpGet]
    public IActionResult Create() => View("Edit", new SportTypeEditViewModel());

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await context.SportTypes.FindAsync(id);
        if (item is null) return NotFound();
        return View(new SportTypeEditViewModel { Name = item.Name, Description = item.Description,
            IconClass = item.IconClass, DisplayOrder = item.DisplayOrder, IsActive = item.IsActive });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create(SportTypeEditViewModel model) => Save(null, model);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(int id, SportTypeEditViewModel model) => Save(id, model);

    private async Task<IActionResult> Save(int? id, SportTypeEditViewModel model)
    {
        var item = id.HasValue ? await context.SportTypes.FindAsync(id.Value) : new SportType();
        if (item is null) return NotFound();
        model.Name = model.Name?.Trim() ?? string.Empty;
        model.Description = model.Description?.Trim();
        model.IconClass = model.IconClass?.Trim();
        if (string.IsNullOrWhiteSpace(model.Name)) ModelState.AddModelError(nameof(model.Name), "Vui lòng nhập tên loại sân.");
        if (await context.SportTypes.AnyAsync(t => t.Name == model.Name && (!id.HasValue || t.Id != id.Value)))
            ModelState.AddModelError(nameof(model.Name), "Tên loại sân đã tồn tại.");
        if (!ModelState.IsValid) return View("Edit", model);
        item.Name = model.Name;
        item.Description = model.Description;
        item.IconClass = model.IconClass;
        item.DisplayOrder = model.DisplayOrder;
        item.IsActive = model.IsActive;
        if (!id.HasValue) context.SportTypes.Add(item);
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            ModelState.AddModelError(nameof(model.Name), "Tên loại sân đã tồn tại.");
            return View("Edit", model);
        }
        TempData["SuccessMessage"] = id.HasValue ? "Đã cập nhật loại sân." : "Đã thêm loại sân.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await context.SportTypes.FindAsync(id);
        if (item is null) return NotFound();
        if (await context.Courts.IgnoreQueryFilters().AnyAsync(c => c.SportTypeId == id))
        {
            TempData["ErrorMessage"] = "Loại sân đang được sử dụng. Bạn có thể ngừng hoạt động thay vì xóa.";
            return RedirectToAction(nameof(Index));
        }
        context.SportTypes.Remove(item);
        try
        {
            await context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa loại sân.";
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
        {
            TempData["ErrorMessage"] = "Loại sân vừa được sử dụng và không thể xóa.";
        }
        return RedirectToAction(nameof(Index));
    }
}
