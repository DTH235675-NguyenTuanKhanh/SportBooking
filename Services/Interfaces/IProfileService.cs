// File: Services/Interfaces/IProfileService.cs
using SportBooking.Models.ViewModels.Profile;

namespace SportBooking.Services.Interfaces;

/// <summary>
/// Nghiệp vụ hồ sơ cá nhân: đọc và cập nhật thông tin người dùng.
/// </summary>
public interface IProfileService
{
    /// <summary>
    /// Lấy thông tin hồ sơ để hiển thị lên form cập nhật.
    /// </summary>
    /// <param name="userId">ID người dùng đang đăng nhập.</param>
    /// <param name="currentRole">Vai trò hiện tại để quyết định hiển thị trường doanh nghiệp.</param>
    Task<ServiceResult<ProfileViewModel>> GetProfileAsync(string userId, string currentRole);

    /// <summary>
    /// Cập nhật hồ sơ. Nếu có AvatarFile thì upload và thay thế ảnh cũ.
    /// </summary>
    Task<ServiceResult> UpdateProfileAsync(string userId, ProfileViewModel model);
}