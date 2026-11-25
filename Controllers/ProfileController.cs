// File: Controllers/ProfileController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SportBooking.Models.Entities;
using SportBooking.Models.ViewModels.Profile;
using SportBooking.Services.Interfaces;

namespace SportBooking.Controllers;

/// <summary>
/// Controller xử lý hồ sơ cá nhân: xem và cập nhật thông tin người dùng.
/// </summary>
[Authorize]
public class ProfileController : Controller
{
    private readonly IProfileService _profileService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        IProfileService profileService,
        UserManager<ApplicationUser> userManager,
        ILogger<ProfileController> logger)
    {
        _profileService = profileService;
        _userManager = userManager;
        _logger = logger;
    }

    // =========================================================================
    // 1. XEM HỒ SƠ
    // =========================================================================
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser is null)
        {
            // Cookie còn hiệu lực nhưng tài khoản đã bị xóa: đăng xuất và yêu cầu đăng nhập lại.
            return RedirectToAction("Login", "Account");
        }

        var currentRole = await GetCurrentRoleAsync(currentUser);

        var result = await _profileService.GetProfileAsync(currentUser.Id, currentRole);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction("Index", "Home");
        }

        // Bổ sung thông tin cần thiết cho View.
        var model = result.Data!;
        model.UserId = currentUser.Id;
        model.CurrentRole = currentRole;
        model.IsApproved = currentUser.ApprovalStatus == OwnerApprovalStatus.Approved;

        return View(model);
    }

    // =========================================================================
    // 2. CẬP NHẬT HỒ SƠ
    // =========================================================================
    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var currentRole = await GetCurrentRoleAsync(currentUser);

        var result = await _profileService.GetProfileAsync(currentUser.Id, currentRole);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        var model = result.Data!;
        model.UserId = currentUser.Id;
        model.CurrentRole = currentRole;
        model.IsApproved = currentUser.ApprovalStatus == OwnerApprovalStatus.Approved;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProfileViewModel model)
    {
        // Lấy vai trò thật của người dùng từ server, không tin giá trị CurrentRole do client gửi lên
        // (chống over-posting: kẻ tấn công có thể sửa hidden field để hiện trường doanh nghiệp).
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser is null)
        {
            return RedirectToAction("Login", "Account");
        }

        model.CurrentRole = await GetCurrentRoleAsync(currentUser);
        model.Email = currentUser.Email!;
        model.IsApproved = currentUser.ApprovalStatus == OwnerApprovalStatus.Approved;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _profileService.UpdateProfileAsync(currentUser.Id, model);

        if (!result.Success)
        {
            // Hiển thị lỗi gắn với từng trường.
            foreach (var (field, messages) in result.Errors)
            {
                foreach (var msg in messages)
                {
                    if (string.IsNullOrEmpty(field))
                    {
                        ModelState.AddModelError(string.Empty, msg);
                    }
                    else
                    {
                        ModelState.AddModelError(field, msg);
                    }
                }
            }

            if (result.Errors.Count == 0)
            {
                ModelState.AddModelError(string.Empty, result.Message);
            }

            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    // =========================================================================
    // HELPER: Lấy vai trò chính của người dùng
    // =========================================================================
    /// <summary>
    /// Trả về role chính của người dùng. Thứ tự ưu tiên Admin > Owner > Customer
    /// để đảm bảo nếu một tài khoản có nhiều role thì vẫn hiển thị đúng quyền cao nhất.
    /// </summary>
    private async Task<string> GetCurrentRoleAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Contains(Constants.AppConstants.Roles.Admin))
        {
            return Constants.AppConstants.Roles.Admin;
        }

        if (roles.Contains(Constants.AppConstants.Roles.Owner))
        {
            return Constants.AppConstants.Roles.Owner;
        }

        return Constants.AppConstants.Roles.Customer;
    }
}