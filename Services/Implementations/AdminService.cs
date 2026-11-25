using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SportBooking.Constants;
using SportBooking.Data;
using SportBooking.Helpers;
using SportBooking.Models.Entities;
using SportBooking.Models.ViewModels.Admin;
using SportBooking.Services.Interfaces;

namespace SportBooking.Services.Implementations;

/// <summary>
/// Nghiệp vụ quản trị nền tảng: dashboard và xét duyệt tài khoản Owner.
/// </summary>
public sealed class AdminService : IAdminService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<AdminService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<AdminDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(ScheduleGuard.Now);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var nextMonth = monthStart.AddMonths(1);
        var chartStart = today.AddDays(-29);
        var paymentRows = await PaymentRevenue.ByDayAsync(_context.Payments.IgnoreQueryFilters().AsNoTracking(),
            chartStart < monthStart ? chartStart : monthStart, today, cancellationToken);
        var revenueByDate = paymentRows.ToDictionary(row => DateOnly.FromDateTime(row.Day), row => row.Net);

        var model = new AdminDashboardViewModel
        {
            TotalUsers = await _context.Users.CountAsync(cancellationToken),
            TotalOwners = await (from user in _context.Users
                                 join userRole in _context.UserRoles on user.Id equals userRole.UserId
                                 join role in _context.Roles on userRole.RoleId equals role.Id
                                 where role.Name == AppConstants.Roles.Owner
                                 select user.Id).Distinct().CountAsync(cancellationToken),
            PendingOwners = await (from user in _context.Users
                                   join userRole in _context.UserRoles on user.Id equals userRole.UserId
                                   join role in _context.Roles on userRole.RoleId equals role.Id
                                   where role.Name == AppConstants.Roles.Owner
                                         && user.ApprovalStatus == OwnerApprovalStatus.Pending
                                   select user.Id).Distinct().CountAsync(cancellationToken),
            TotalComplexes = await _context.SportComplexes.CountAsync(cancellationToken),
            TotalCourts = await _context.Courts.CountAsync(cancellationToken),
            TodayBookings = await _context.Bookings.CountAsync(b => b.BookingDate == today, cancellationToken),
            MonthBookings = await _context.Bookings.CountAsync(
                b => b.BookingDate >= monthStart && b.BookingDate < nextMonth, cancellationToken),
            MonthRevenue = revenueByDate.Where(row => row.Key >= monthStart).Sum(row => row.Value)
        };

        model.PendingOwnersLatest = (await GetOwnerQuery(OwnerApprovalStatus.Pending)
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .ToListAsync(cancellationToken))
            .Select(MapOwner)
            .ToList();

        model.LatestReviews = await _context.Reviews
            .AsNoTracking()
            .Include(r => r.Customer)
            .Include(r => r.Court)
                .ThenInclude(c => c.SportComplex)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .Select(r => new AdminReviewItemViewModel
            {
                Id = r.Id,
                Rating = r.Rating,
                Comment = r.Comment,
                IsVisible = r.IsVisible,
                CustomerName = r.Customer.FullName,
                CourtName = r.Court.Name,
                ComplexName = r.Court.SportComplex.Name,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var chartRows = await _context.Bookings
            .AsNoTracking()
            .Where(b => b.BookingDate >= chartStart && b.BookingDate <= today)
            .GroupBy(b => b.BookingDate)
            .Select(g => new
            {
                Date = g.Key,
                BookingCount = g.Count()
            })
            .ToListAsync(cancellationToken);

        var chartByDate = chartRows.ToDictionary(x => x.Date, x => x.BookingCount);
        model.BookingChart = Enumerable.Range(0, 30)
            .Select(offset => chartStart.AddDays(offset))
            .Select(date => new AdminChartPointViewModel
            {
                Label = date.ToString("dd/MM"), BookingCount = chartByDate.GetValueOrDefault(date),
                Revenue = revenueByDate.GetValueOrDefault(date)
            })
            .ToList();

        return model;
    }

    public async Task<OwnerApprovalListViewModel> GetOwnerApprovalsAsync(
        OwnerApprovalListViewModel filter,
        CancellationToken cancellationToken = default)
    {
        filter.Page = Math.Max(1, filter.Page);
        filter.PageSize = filter.PageSize is < 1 or > 50 ? 10 : filter.PageSize;
        if (!Enum.TryParse<OwnerApprovalStatus>(filter.Status, true, out var status)
            || status == OwnerApprovalStatus.NotApplicable)
        {
            status = OwnerApprovalStatus.Pending;
            filter.Status = nameof(OwnerApprovalStatus.Pending);
        }

        var query = GetOwnerQuery(status);
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim();
            query = query.Where(u => u.FullName.Contains(keyword)
                                     || (u.Email != null && u.Email.Contains(keyword))
                                     || (u.BusinessName != null && u.BusinessName.Contains(keyword)));
        }

        filter.TotalCount = await query.CountAsync(cancellationToken);
        var totalPages = filter.TotalPages;
        if (totalPages > 0 && filter.Page > totalPages)
        {
            filter.Page = totalPages;
        }

        filter.Owners = (await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken))
            .Select(MapOwner)
            .ToList();

        return filter;
    }

    public async Task<OwnerApprovalDetailViewModel?> GetOwnerApprovalDetailAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await GetOwnerQuery(null)
            .Where(u => u.Id == userId)
            .Select(u => new OwnerApprovalDetailViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                PhoneNumber = u.PhoneNumber,
                BusinessName = u.BusinessName,
                BusinessPhone = u.BusinessPhone,
                Address = u.Address,
                DateOfBirth = u.DateOfBirth,
                ApprovalStatus = u.ApprovalStatus,
                CreatedAt = u.CreatedAt,
                ApprovalReviewedAt = u.ApprovalReviewedAt,
                ApprovalRejectionReason = u.ApprovalRejectionReason,
                ApprovalReviewerName = u.ApprovalReviewedById == null
                    ? null
                    : _context.Users.Where(reviewer => reviewer.Id == u.ApprovalReviewedById)
                        .Select(reviewer => reviewer.FullName).FirstOrDefault(),
                ComplexCount = u.SportComplexes.Count,
                CourtCount = u.SportComplexes.SelectMany(c => c.Courts).Count()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ServiceResult> DecideOwnerApprovalAsync(
        string adminId,
        OwnerApprovalDecisionViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (model.Decision != OwnerApprovalStatus.Approved
            && model.Decision != OwnerApprovalStatus.Rejected)
        {
            return ServiceResult.Fail("Quyết định duyệt không hợp lệ.");
        }

        if (model.Decision == OwnerApprovalStatus.Rejected
            && string.IsNullOrWhiteSpace(model.RejectionReason))
        {
            return ServiceResult.Fail("Vui lòng nhập lý do từ chối.");
        }

        var owner = await GetOwnerQuery(null)
            .FirstOrDefaultAsync(u => u.Id == model.UserId, cancellationToken);
        if (owner is null)
        {
            return ServiceResult.Fail("Không tìm thấy tài khoản chủ sân.");
        }

        if (owner.ApprovalStatus != OwnerApprovalStatus.Pending)
        {
            return ServiceResult.Fail("Yêu cầu này đã được Admin khác xử lý trước đó.");
        }

        var now = DateTime.UtcNow;
        var updated = await _context.Users
            .Where(u => u.Id == model.UserId
                        && u.ApprovalStatus == OwnerApprovalStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.ApprovalStatus, model.Decision)
                .SetProperty(u => u.ApprovalReviewedAt, now)
                .SetProperty(u => u.ApprovalReviewedById, adminId)
                .SetProperty(u => u.ApprovalRejectionReason,
                    model.Decision == OwnerApprovalStatus.Rejected
                        ? model.RejectionReason!.Trim()
                        : null)
                .SetProperty(u => u.UpdatedAt, now), cancellationToken);

        if (updated != 1)
        {
            return ServiceResult.Fail("Yêu cầu vừa được xử lý bởi Admin khác. Vui lòng tải lại trang.");
        }

        _logger.LogInformation("Admin {AdminId} chuyển trạng thái Owner {OwnerId} sang {Status}.",
            adminId, model.UserId, model.Decision);

        return ServiceResult.Ok(model.Decision == OwnerApprovalStatus.Approved
            ? "Đã duyệt tài khoản chủ sân."
            : "Đã từ chối tài khoản chủ sân.");
    }

    public async Task<AdminUserListViewModel> GetUsersAsync(
        AdminUserListViewModel filter,
        CancellationToken cancellationToken = default)
    {
        filter.Page = Math.Max(1, filter.Page);
        filter.PageSize = filter.PageSize is < 1 or > 50 ? 10 : filter.PageSize;
        filter.RoleFilter = NormalizeRoleFilter(filter.RoleFilter);
        filter.StatusFilter = NormalizeUserStatusFilter(filter.StatusFilter);

        var now = DateTimeOffset.UtcNow;
        var query = _context.Users.AsNoTracking().AsQueryable();

        if (!string.Equals(filter.RoleFilter, "All", StringComparison.OrdinalIgnoreCase))
        {
            var roleName = filter.RoleFilter;
            query = query.Where(user =>
                (from userRole in _context.UserRoles
                 join role in _context.Roles on userRole.RoleId equals role.Id
                 where userRole.UserId == user.Id && role.Name == roleName
                 select userRole.UserId).Any());
        }

        if (string.Equals(filter.StatusFilter, "Active", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(user => user.IsActive);
        }
        else if (string.Equals(filter.StatusFilter, "Inactive", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(user => !user.IsActive);
        }
        else if (string.Equals(filter.StatusFilter, "TemporarilyLocked", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(user => user.LockoutEnd.HasValue && user.LockoutEnd > now);
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim();
            query = query.Where(user => user.FullName.Contains(keyword)
                                        || (user.Email != null && user.Email.Contains(keyword))
                                        || (user.PhoneNumber != null && user.PhoneNumber.Contains(keyword)));
        }

        filter.TotalCount = await query.CountAsync(cancellationToken);
        var totalPages = filter.TotalPages;
        if (totalPages > 0 && filter.Page > totalPages)
        {
            filter.Page = totalPages;
        }

        var rows = await query
            .OrderByDescending(user => user.CreatedAt)
            .ThenBy(user => user.Email)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(user => new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.PhoneNumber,
                user.IsActive,
                user.LockoutEnd,
                user.ApprovalStatus,
                user.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var userIds = rows.Select(row => row.Id).ToList();
        var roleRows = await (from userRole in _context.UserRoles.AsNoTracking()
                              join role in _context.Roles.AsNoTracking()
                                  on userRole.RoleId equals role.Id
                              where userIds.Contains(userRole.UserId)
                              select new { userRole.UserId, RoleName = role.Name })
            .ToListAsync(cancellationToken);

        var rolesByUser = roleRows
            .Where(row => row.RoleName != null)
            .GroupBy(row => row.UserId)
            .ToDictionary(group => group.Key, group => group
                .Select(row => row.RoleName!)
                .OrderBy(role => role)
                .ToList());

        filter.Users = rows.Select(row => new AdminUserItemViewModel
        {
            Id = row.Id,
            FullName = row.FullName,
            Email = row.Email ?? string.Empty,
            PhoneNumber = row.PhoneNumber,
            IsActive = row.IsActive,
            IsTemporarilyLocked = row.LockoutEnd.HasValue && row.LockoutEnd > now,
            LockoutEnd = row.LockoutEnd,
            ApprovalStatus = row.ApprovalStatus,
            CreatedAt = row.CreatedAt,
            Roles = rolesByUser.GetValueOrDefault(row.Id) ?? new List<string>()
        }).ToList();

        return filter;
    }

    public async Task<AdminUserDetailViewModel?> GetUserDetailAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var roles = await (from userRole in _context.UserRoles.AsNoTracking()
                           join role in _context.Roles.AsNoTracking()
                               on userRole.RoleId equals role.Id
                           where userRole.UserId == userId && role.Name != null
                           orderby role.Name
                           select role.Name!)
            .ToListAsync(cancellationToken);

        var complexRows = await _context.SportComplexes
            .AsNoTracking()
            .Where(complex => complex.OwnerId == userId)
            .OrderBy(complex => complex.Name)
            .Select(complex => new AdminComplexItemViewModel
            {
                Id = complex.Id,
                Name = complex.Name,
                Address = complex.Address,
                IsActive = complex.IsActive
            })
            .ToListAsync(cancellationToken);

        var complexIds = complexRows.Select(complex => complex.Id).ToList();
        var courtCounts = complexIds.Count == 0
            ? new Dictionary<int, int>()
            : await _context.Courts
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(court => complexIds.Contains(court.SportComplexId))
                .GroupBy(court => court.SportComplexId)
                .Select(group => new { ComplexId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(row => row.ComplexId, row => row.Count, cancellationToken);

        foreach (var complex in complexRows)
        {
            complex.CourtCount = courtCounts.GetValueOrDefault(complex.Id, 0);
        }

        var bookingQuery = _context.Bookings
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(booking => booking.CustomerId == userId
                              || booking.Court.SportComplex.OwnerId == userId);

        var bookingCount = await bookingQuery.CountAsync(cancellationToken);
        var bookings = await bookingQuery
            .OrderByDescending(booking => booking.BookingDate)
            .ThenByDescending(booking => booking.StartDateTime)
            .Take(50)
            .Select(booking => new AdminBookingItemViewModel
            {
                Id = booking.Id,
                BookingCode = booking.BookingCode,
                BookingDate = booking.BookingDate,
                CourtName = booking.Court.Name,
                ComplexName = booking.Court.SportComplex.Name,
                TotalAmount = booking.TotalAmount,
                Status = booking.Status,
                PaymentStatus = booking.PaymentStatus
            })
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        return new AdminUserDetailViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            IsTemporarilyLocked = user.LockoutEnd.HasValue && user.LockoutEnd > now,
            LockoutEnd = user.LockoutEnd,
            ApprovalStatus = user.ApprovalStatus,
            CreatedAt = user.CreatedAt,
            Roles = roles,
            DateOfBirth = user.DateOfBirth,
            Address = user.Address,
            BusinessName = user.BusinessName,
            BusinessPhone = user.BusinessPhone,
            UpdatedAt = user.UpdatedAt,
            ComplexCount = complexRows.Count,
            BookingCount = bookingCount,
            Complexes = complexRows,
            Bookings = bookings
        };
    }

    public async Task<ServiceResult> SetUserActiveAsync(
        string adminId,
        string userId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            // Mỗi lần thử lại phải đọc tài khoản và SecurityStamp mới từ database.
            _context.ChangeTracker.Clear();
            return await SetUserActiveInTransactionAsync(adminId, userId, isActive, cancellationToken);
        });
    }

    private async Task<ServiceResult> SetUserActiveInTransactionAsync(
        string adminId,
        string userId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(adminId) || string.IsNullOrWhiteSpace(userId))
        {
            return ServiceResult.Fail("Thông tin tài khoản không hợp lệ.");
        }

        if (!isActive && string.Equals(adminId, userId, StringComparison.Ordinal))
        {
            return ServiceResult.Fail("Bạn không thể tự khóa tài khoản Admin của mình.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);

        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
            if (user is null)
            {
                return ServiceResult.Fail("Không tìm thấy tài khoản.");
            }

            var hasTemporaryLock = user.LockoutEnd.HasValue
                                   && user.LockoutEnd > DateTimeOffset.UtcNow;
            if (user.IsActive == isActive && (!isActive || !hasTemporaryLock))
            {
                await transaction.CommitAsync(cancellationToken);
                return ServiceResult.Ok(isActive
                    ? "Tài khoản đã đang hoạt động."
                    : "Tài khoản đã bị khóa.");
            }

            var isAdmin = await (from userRole in _context.UserRoles
                                 join role in _context.Roles
                                     on userRole.RoleId equals role.Id
                                 where userRole.UserId == userId
                                       && role.Name == AppConstants.Roles.Admin
                                 select userRole.UserId).AnyAsync(cancellationToken);

            if (!isActive && isAdmin)
            {
                var activeAdminCount = await (from candidate in _context.Users
                                              join userRole in _context.UserRoles
                                                  on candidate.Id equals userRole.UserId
                                              join role in _context.Roles
                                                  on userRole.RoleId equals role.Id
                                              where candidate.IsActive
                                                    && role.Name == AppConstants.Roles.Admin
                                              select candidate.Id)
                    .Distinct()
                    .CountAsync(cancellationToken);

                if (activeAdminCount <= 1)
                {
                    return ServiceResult.Fail("Không thể khóa Admin cuối cùng đang hoạt động.");
                }
            }

            user.IsActive = isActive;
            user.UpdatedAt = DateTime.UtcNow;
            if (isActive)
            {
                // Mở khóa quản trị phải xóa cả khóa tạm thời do Identity,
                // nếu không tài khoản vẫn có thể bị từ chối đăng nhập.
                user.LockoutEnd = null;
                user.AccessFailedCount = 0;
            }

            var updateResult = await _userManager.UpdateSecurityStampAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join(" ", updateResult.Errors.Select(error => error.Description));
                return ServiceResult.Fail($"Không thể cập nhật tài khoản: {errors}");
            }

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Admin {AdminId} {Action} tài khoản {UserId}.",
                adminId, isActive ? "mở khóa" : "khóa", userId);

            return ServiceResult.Ok(isActive
                ? "Đã mở khóa tài khoản."
                : "Đã khóa tài khoản và vô hiệu các phiên đăng nhập hiện tại.");
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Xung đột khi Admin {AdminId} cập nhật tài khoản {UserId}.", adminId, userId);
            return ServiceResult.Fail("Tài khoản vừa được cập nhật bởi người khác. Vui lòng tải lại trang.");
        }
    }

    public async Task<AdminReviewListViewModel> GetReviewsAsync(
        AdminReviewListViewModel filter,
        CancellationToken cancellationToken = default)
    {
        filter.Page = Math.Max(1, filter.Page);
        filter.PageSize = filter.PageSize is < 1 or > 50 ? 10 : filter.PageSize;
        filter.VisibilityFilter = NormalizeVisibilityFilter(filter.VisibilityFilter);
        filter.CourtId = filter.CourtId is > 0 ? filter.CourtId : null;
        filter.Rating = filter.Rating is >= 1 and <= 5 ? filter.Rating : null;

        var query = _context.Reviews
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AsQueryable();

        if (filter.CourtId.HasValue)
        {
            query = query.Where(review => review.CourtId == filter.CourtId.Value);
        }

        if (filter.Rating.HasValue)
        {
            query = query.Where(review => review.Rating == filter.Rating.Value);
        }

        if (string.Equals(filter.VisibilityFilter, "Visible", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(review => review.IsVisible);
        }
        else if (string.Equals(filter.VisibilityFilter, "Hidden", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(review => !review.IsVisible);
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim();
            query = query.Where(review => (review.Comment != null && review.Comment.Contains(keyword))
                                          || (review.Customer.FullName != null && review.Customer.FullName.Contains(keyword))
                                          || (review.Customer.Email != null && review.Customer.Email.Contains(keyword))
                                          || review.Court.Name.Contains(keyword)
                                          || review.Court.SportComplex.Name.Contains(keyword)
                                          || review.Booking.BookingCode.Contains(keyword));
        }

        filter.TotalCount = await query.CountAsync(cancellationToken);
        var totalPages = filter.TotalPages;
        if (totalPages > 0 && filter.Page > totalPages)
        {
            filter.Page = totalPages;
        }

        filter.Reviews = await query
            .OrderByDescending(review => review.CreatedAt)
            .ThenByDescending(review => review.Id)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(review => new AdminReviewItemViewModel
            {
                Id = review.Id,
                Rating = review.Rating,
                Comment = review.Comment,
                OwnerReply = review.OwnerReply,
                IsVisible = review.IsVisible,
                CustomerName = review.Customer.FullName,
                CustomerEmail = review.Customer.Email,
                CourtName = review.Court.Name,
                ComplexName = review.Court.SportComplex.Name,
                BookingCode = review.Booking.BookingCode,
                CreatedAt = review.CreatedAt,
                UpdatedAt = review.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        filter.Courts = await _context.Courts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .OrderBy(court => court.SportComplex.Name)
            .ThenBy(court => court.Name)
            .Select(court => new AdminCourtOptionViewModel
            {
                Id = court.Id,
                DisplayName = court.SportComplex.Name + " - " + court.Name
            })
            .ToListAsync(cancellationToken);

        return filter;
    }

    public async Task<ServiceResult> SetReviewVisibilityAsync(
        int reviewId,
        bool isVisible,
        CancellationToken cancellationToken = default)
    {
        if (reviewId <= 0)
        {
            return ServiceResult.Fail("Đánh giá không hợp lệ.");
        }

        var updated = await _context.Reviews
            .Where(review => review.Id == reviewId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(review => review.IsVisible, isVisible)
                .SetProperty(review => review.UpdatedAt, DateTime.UtcNow), cancellationToken);

        if (updated != 1)
        {
            return ServiceResult.Fail("Không tìm thấy đánh giá hoặc đánh giá đã được cập nhật.");
        }

        _logger.LogInformation("Admin cập nhật đánh giá {ReviewId}: IsVisible={IsVisible}.",
            reviewId, isVisible);
        return ServiceResult.Ok(isVisible ? "Đã hiển thị đánh giá." : "Đã ẩn đánh giá.");
    }

    private static string NormalizeRoleFilter(string? value)
    {
        return AppConstants.Roles.All.FirstOrDefault(role =>
                   string.Equals(role, value, StringComparison.OrdinalIgnoreCase))
               ?? "All";
    }

    private static string NormalizeUserStatusFilter(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "active" => "Active",
            "inactive" => "Inactive",
            "temporarilylocked" => "TemporarilyLocked",
            _ => "All"
        };
    }

    private static string NormalizeVisibilityFilter(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "visible" => "Visible",
            "hidden" => "Hidden",
            _ => "All"
        };
    }

    private IQueryable<ApplicationUser> GetOwnerQuery(OwnerApprovalStatus? status)
    {
        var query = from user in _context.Users.AsNoTracking()
                    join userRole in _context.UserRoles on user.Id equals userRole.UserId
                    join role in _context.Roles on userRole.RoleId equals role.Id
                    where role.Name == AppConstants.Roles.Owner
                    select user;

        return status.HasValue
            ? query.Where(u => u.ApprovalStatus == status.Value)
            : query;
    }

    private static PendingOwnerItemViewModel MapOwner(ApplicationUser user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email ?? string.Empty,
        PhoneNumber = user.PhoneNumber,
        BusinessName = user.BusinessName,
        BusinessPhone = user.BusinessPhone,
        Address = user.Address,
        ApprovalStatus = user.ApprovalStatus,
        CreatedAt = user.CreatedAt,
        ApprovalReviewedAt = user.ApprovalReviewedAt,
        ApprovalRejectionReason = user.ApprovalRejectionReason
    };
}
