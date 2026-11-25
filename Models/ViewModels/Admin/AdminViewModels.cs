using System.ComponentModel.DataAnnotations;
using SportBooking.Models.Entities;

namespace SportBooking.Models.ViewModels.Admin;

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int TotalOwners { get; set; }
    public int PendingOwners { get; set; }
    public int TotalComplexes { get; set; }
    public int TotalCourts { get; set; }
    public int TodayBookings { get; set; }
    public int MonthBookings { get; set; }
    public decimal MonthRevenue { get; set; }
    public List<PendingOwnerItemViewModel> PendingOwnersLatest { get; set; } = new();
    public List<AdminReviewItemViewModel> LatestReviews { get; set; } = new();
    public List<AdminChartPointViewModel> BookingChart { get; set; } = new();
}

public class AdminChartPointViewModel
{
    public string Label { get; set; } = string.Empty;
    public int BookingCount { get; set; }
    public decimal Revenue { get; set; }
}

public class OwnerApprovalListViewModel
{
    public List<PendingOwnerItemViewModel> Owners { get; set; } = new();
    public string Status { get; set; } = nameof(OwnerApprovalStatus.Pending);
    public string? Keyword { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

public class PendingOwnerItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? BusinessName { get; set; }
    public string? BusinessPhone { get; set; }
    public string? Address { get; set; }
    public OwnerApprovalStatus ApprovalStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovalReviewedAt { get; set; }
    public string? ApprovalRejectionReason { get; set; }
    public string? ApprovalReviewerName { get; set; }
}

public class OwnerApprovalDetailViewModel : PendingOwnerItemViewModel
{
    public DateTime? DateOfBirth { get; set; }
    public int ComplexCount { get; set; }
    public int CourtCount { get; set; }
}

public class OwnerApprovalDecisionViewModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public OwnerApprovalStatus Decision { get; set; }

    [StringLength(1000)]
    public string? RejectionReason { get; set; }
}

public class AdminReviewItemViewModel
{
    public int Id { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string? OwnerReply { get; set; }
    public bool IsVisible { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string CourtName { get; set; } = string.Empty;
    public string ComplexName { get; set; } = string.Empty;
    public string BookingCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class AdminUserListViewModel
{
    public List<AdminUserItemViewModel> Users { get; set; } = new();
    public string RoleFilter { get; set; } = "All";
    public string StatusFilter { get; set; } = "All";
    public string? Keyword { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0
        ? (int)Math.Ceiling(TotalCount / (double)PageSize)
        : 0;
}

public class AdminUserItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public bool IsTemporarilyLocked { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public OwnerApprovalStatus ApprovalStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class AdminUserDetailViewModel : AdminUserItemViewModel
{
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? BusinessName { get; set; }
    public string? BusinessPhone { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int ComplexCount { get; set; }
    public int BookingCount { get; set; }
    public List<AdminComplexItemViewModel> Complexes { get; set; } = new();
    public List<AdminBookingItemViewModel> Bookings { get; set; } = new();
}

public class AdminComplexItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int CourtCount { get; set; }
}

public class AdminBookingItemViewModel
{
    public int Id { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public DateOnly BookingDate { get; set; }
    public string CourtName { get; set; } = string.Empty;
    public string ComplexName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public BookingStatus Status { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
}

public class AdminReviewListViewModel
{
    public List<AdminReviewItemViewModel> Reviews { get; set; } = new();
    public List<AdminCourtOptionViewModel> Courts { get; set; } = new();
    public int? CourtId { get; set; }
    public int? Rating { get; set; }
    public string VisibilityFilter { get; set; } = "All";
    public string? Keyword { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0
        ? (int)Math.Ceiling(TotalCount / (double)PageSize)
        : 0;
}

public class AdminCourtOptionViewModel
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
