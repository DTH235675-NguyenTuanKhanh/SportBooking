using SportBooking.Models.Entities;
using SportBooking.Models.ViewModels.Admin;

namespace SportBooking.Services.Interfaces;

public interface IAdminService
{
    Task<AdminDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<OwnerApprovalListViewModel> GetOwnerApprovalsAsync(OwnerApprovalListViewModel filter, CancellationToken cancellationToken = default);
    Task<OwnerApprovalDetailViewModel?> GetOwnerApprovalDetailAsync(string userId, CancellationToken cancellationToken = default);
    Task<ServiceResult> DecideOwnerApprovalAsync(string adminId, OwnerApprovalDecisionViewModel model, CancellationToken cancellationToken = default);
    Task<AdminUserListViewModel> GetUsersAsync(AdminUserListViewModel filter, CancellationToken cancellationToken = default);
    Task<AdminUserDetailViewModel?> GetUserDetailAsync(string userId, CancellationToken cancellationToken = default);
    Task<ServiceResult> SetUserActiveAsync(string adminId, string userId, bool isActive, CancellationToken cancellationToken = default);
    Task<AdminReviewListViewModel> GetReviewsAsync(AdminReviewListViewModel filter, CancellationToken cancellationToken = default);
    Task<ServiceResult> SetReviewVisibilityAsync(int reviewId, bool isVisible, CancellationToken cancellationToken = default);
}
