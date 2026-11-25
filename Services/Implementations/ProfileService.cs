// File: Services/Implementations/ProfileService.cs
using Microsoft.AspNetCore.Identity;
using SportBooking.Constants;
using SportBooking.Models.Entities;
using SportBooking.Models.ViewModels.Profile;
using SportBooking.Services.Interfaces;

namespace SportBooking.Services.Implementations;

/// <summary>
/// Nghiệp vụ hồ sơ cá nhân.
/// Đọc và cập nhật thông tin người dùng, bao gồm upload ảnh đại diện.
/// </summary>
public class ProfileService : IProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(
        UserManager<ApplicationUser> userManager,
        IFileUploadService fileUploadService,
        ILogger<ProfileService> logger)
    {
        _userManager = userManager;
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    public async Task<ServiceResult<ProfileViewModel>> GetProfileAsync(string userId, string currentRole)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult<ProfileViewModel>.Fail("Không tìm thấy tài khoản.");
        }

        var isOwner = currentRole == AppConstants.Roles.Owner;

        var viewModel = new ProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            DateOfBirth = user.DateOfBirth,
            Address = user.Address,
            AvatarUrl = user.AvatarUrl,
            BusinessName = isOwner ? user.BusinessName : null,
            BusinessPhone = isOwner ? user.BusinessPhone : null
        };

        return ServiceResult<ProfileViewModel>.Ok(viewModel);
    }

    public async Task<ServiceResult> UpdateProfileAsync(string userId, ProfileViewModel model)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult.Fail("Không tìm thấy tài khoản.");
        }

        // ---------- 1. Cập nhật các trường văn bản ----------
        user.FullName = model.FullName.Trim();
        user.PhoneNumber = model.PhoneNumber.Trim();
        user.DateOfBirth = model.DateOfBirth;
        user.Address = model.Address?.Trim();

        // Nếu là Chủ sân, cho phép cập nhật thông tin doanh nghiệp.
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains(AppConstants.Roles.Owner))
        {
            user.BusinessName = model.BusinessName?.Trim();
            user.BusinessPhone = model.BusinessPhone?.Trim();
        }

        // ---------- 2. Xử lý upload ảnh đại diện mới ----------
        if (model.AvatarFile is not null)
        {
            var uploadResult = await _fileUploadService.SaveImageAsync(model.AvatarFile, "avatars");

            if (!uploadResult.Success)
            {
                return ServiceResult.Fail(new Dictionary<string, string[]>
                {
                    [nameof(ProfileViewModel.AvatarFile)] = new[] { uploadResult.ErrorMessage ?? "Lỗi khi tải ảnh lên." }
                }, "Cập nhật hồ sơ không thành công.");
            }

            // Xóa ảnh cũ (nếu có).
            if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
            {
                _fileUploadService.DeleteImage(user.AvatarUrl);
            }

            user.AvatarUrl = uploadResult.RelativePath;

            _logger.LogInformation("Đã cập nhật avatar cho {Email}: {AvatarUrl}", user.Email, user.AvatarUrl);
        }

        // ---------- 3. Lưu thay đổi ----------
        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            _logger.LogError("Cập nhật hồ sơ thất bại cho {Email}: {Errors}",
                user.Email,
                string.Join(", ", updateResult.Errors.Select(e => e.Description)));

            return ServiceResult.Fail("Cập nhật hồ sơ không thành công. Vui lòng thử lại.");
        }

        _logger.LogInformation("Cập nhật hồ sơ thành công cho {Email}.", user.Email);

        return ServiceResult.Ok("Cập nhật hồ sơ thành công.");
    }
}