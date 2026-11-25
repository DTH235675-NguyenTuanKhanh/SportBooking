// File: Services/Implementations/CourtService.cs
using Microsoft.EntityFrameworkCore;
using SportBooking.Data;
using SportBooking.Helpers;
using SportBooking.Models.Entities;
using SportBooking.Models.ViewModels.Court;
using SportBooking.Models.ViewModels.Home;
using SportBooking.Services.Interfaces;

namespace SportBooking.Services.Implementations;

/// <summary>
/// Nghiệp vụ sân: tìm kiếm, chi tiết, dữ liệu trang chủ.
/// Toàn bộ truy vấn đọc đều dùng AsNoTracking để tối ưu hiệu năng.
/// </summary>
public class CourtService : ICourtService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CourtService> _logger;

    public CourtService(ApplicationDbContext context, ILogger<CourtService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // =========================================================================
    // 1. TÌM KIẾM SÂN
    // =========================================================================
    public async Task<CourtSearchViewModel> SearchCourtsAsync(CourtSearchViewModel filter)
    {
        // Chuẩn hóa tham số phân trang để tránh giá trị bất thường từ query string.
        if (filter.Page < 1)
        {
            filter.Page = 1;
        }

        if (filter.PageSize < 1 || filter.PageSize > 48)
        {
            filter.PageSize = 12;
        }

        // Truy vấn gốc: chỉ lấy sân đang hoạt động, chưa xóa mềm.
        // Query filter IsDeleted đã được cấu hình ở DbContext nên không cần thêm điều kiện.
        var query = _context.Courts
            .AsNoTracking()
            .Include(c => c.SportType)
            .Include(c => c.SportComplex)
            .Where(c => c.Status == CourtStatus.Active
                        && c.SportComplex.IsActive && c.SportComplex.Owner.IsActive
                        && c.SportComplex.Owner.ApprovalStatus == OwnerApprovalStatus.Approved
                        && c.SportType.IsActive);

        // --- Lọc theo từ khóa ---
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim();
            query = query.Where(c =>
                c.Name.Contains(keyword) ||
                c.SportComplex.Name.Contains(keyword) ||
                (c.Description != null && c.Description.Contains(keyword)) ||
                c.SportComplex.Address.Contains(keyword));
        }

        // --- Lọc theo loại sân ---
        if (filter.SportTypeId.HasValue && filter.SportTypeId.Value > 0)
        {
            query = query.Where(c => c.SportTypeId == filter.SportTypeId.Value);
        }

        // --- Lọc theo khu vực ---
        if (!string.IsNullOrWhiteSpace(filter.Province))
        {
            query = query.Where(c => c.SportComplex.Province == filter.Province);
        }

        if (!string.IsNullOrWhiteSpace(filter.District))
        {
            query = query.Where(c => c.SportComplex.District == filter.District);
        }

        // --- Lọc theo khoảng giá ---
        // Giá của sân nằm ở các TimeSlot; dùng Min/Max của khung giờ ngày thường làm căn cứ lọc.
        if (filter.MinPrice.HasValue && filter.MinPrice.Value > 0)
        {
            query = query.Where(c => c.TimeSlots.Any(t => t.IsActive && t.PriceWeekday >= filter.MinPrice.Value));
        }

        if (filter.MaxPrice.HasValue && filter.MaxPrice.Value > 0)
        {
            query = query.Where(c => c.TimeSlots.Any(t => t.IsActive && t.PriceWeekday <= filter.MaxPrice.Value));
        }

        // --- Lọc theo ngày + khung giờ còn trống ---
        // Chỉ giữ sân có ít nhất một khung giờ chưa bị đặt trong ngày được chọn.
        IQueryable<TimeSlot>? availableOnDate = null;
        if (filter.BookingDate.HasValue)
        {
            var dateValue = DateOnly.FromDateTime(filter.BookingDate.Value);
            var now = ScheduleGuard.Now;
            var today = DateOnly.FromDateTime(now);
            var currentTime = now.TimeOfDay;

            // Các trạng thái đang giữ chỗ (Pending, Confirmed, Completed).
            var holdingStatuses = new[]
            {
                BookingStatus.Pending,
                BookingStatus.Confirmed,
                BookingStatus.Completed
            };

            availableOnDate = _context.TimeSlots.AsNoTracking().Where(t => t.IsActive
                && (dateValue > today || (dateValue == today && t.StartTime > currentTime))
                && (!t.Court.SportComplex.OpeningTime.HasValue ||
                    (t.StartTime >= t.Court.SportComplex.OpeningTime && t.EndTime <= t.Court.SportComplex.ClosingTime))
                && !t.Court.Bookings.Any(b => b.BookingDate == dateValue
                    && holdingStatuses.Contains(b.Status)
                    && b.StartDateTime.TimeOfDay < t.EndTime && b.EndDateTime.TimeOfDay > t.StartTime));

            // Dropdown dùng số phút từ đầu ngày, không phải khóa chính TimeSlot.
            var matchingSlots = availableOnDate;
            if (filter.TimeSlotId is > 0)
            {
                var startTime = TimeSpan.FromMinutes(filter.TimeSlotId.Value);
                matchingSlots = matchingSlots.Where(t => t.StartTime == startTime);
            }

            query = query.Where(c => matchingSlots.Any(t => t.CourtId == c.Id));
        }

        // --- Đếm tổng kết quả (trước khi phân trang) ---
        filter.TotalResults = await query.CountAsync();

        // --- Sắp xếp ---
        query = filter.SortBy switch
        {
            "price_asc" => query.OrderBy(c => c.TimeSlots.Where(t => t.IsActive).Min(t => (decimal?)t.PriceWeekday) ?? 0),
            "price_desc" => query.OrderByDescending(c => c.TimeSlots.Where(t => t.IsActive).Min(t => (decimal?)t.PriceWeekday) ?? 0),
            "newest" => query.OrderByDescending(c => c.CreatedAt),
            _ => query.OrderByDescending(c => c.Reviews.Where(r => r.IsVisible).Average(r => (double?)r.Rating) ?? 0)
        };

        // --- Phân trang phía server ---
        var courts = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                c.ImageUrl,
                c.Status,
                ComplexName = c.SportComplex.Name,
                Province = c.SportComplex.Province,
                District = c.SportComplex.District,
                SportTypeName = c.SportType.Name,
                SportTypeIcon = c.SportType.IconClass,
                MinPrice = c.TimeSlots.Where(t => t.IsActive).Min(t => (decimal?)t.PriceWeekday) ?? 0,
                MaxPrice = c.TimeSlots.Where(t => t.IsActive).Max(t => (decimal?)t.PriceWeekend) ?? 0,
                AverageRating = c.Reviews.Where(r => r.IsVisible).Average(r => (double?)r.Rating) ?? 0,
                ReviewCount = c.Reviews.Count(r => r.IsVisible)
            })
            .ToListAsync();

        // --- Tính số khung giờ còn trống nếu có lọc theo ngày ---
        var availableCounts = new Dictionary<int, int>();
        if (availableOnDate is not null && courts.Count > 0)
        {
            var courtIds = courts.Select(c => c.Id).ToList();
            availableCounts = await availableOnDate
                .Where(t => courtIds.Contains(t.CourtId))
                .GroupBy(t => t.CourtId)
                .Select(g => new { CourtId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CourtId, x => x.Count);
        }

        // --- Map sang ViewModel ---
        filter.Courts = courts.Select(c => new CourtCardViewModel
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            ImageUrl = c.ImageUrl,
            ComplexName = c.ComplexName,
            Location = $"{c.District}, {c.Province}",
            SportTypeName = c.SportTypeName,
            SportTypeIcon = c.SportTypeIcon,
            MinPrice = c.MinPrice,
            MaxPrice = c.MaxPrice,
            AverageRating = Math.Round(c.AverageRating, 1),
            ReviewCount = c.ReviewCount,
            Status = (int)c.Status,
            AvailableSlotsCount = availableCounts.GetValueOrDefault(c.Id, 0)
        }).ToList();

        // --- Nạp dữ liệu cho các dropdown filter ---
        filter.SportTypes = await GetSportTypesAsync();
        filter.Provinces = await GetProvincesAsync();
        filter.TimeSlots = await GetTimeSlotOptionsAsync();

        _logger.LogInformation("Tìm kiếm sân: tìm thấy {Count} kết quả (trang {Page}/{TotalPages}).",
            filter.TotalResults, filter.Page, filter.TotalPages);

        return filter;
    }

    // =========================================================================
    // 2. CHI TIẾT SÂN
    // =========================================================================
    public async Task<CourtDetailViewModel?> GetCourtDetailAsync(int courtId, int reviewPage = 1, int reviewPageSize = 5)
    {
        if (reviewPage < 1)
        {
            reviewPage = 1;
        }

        if (reviewPageSize < 1 || reviewPageSize > 20)
        {
            reviewPageSize = 5;
        }

        // --- Lấy thông tin sân kèm cụm sân và loại sân ---
        var court = await _context.Courts
            .AsNoTracking()
            .Include(c => c.SportType)
            .Include(c => c.SportComplex)
                .ThenInclude(complex => complex.Owner)
            .FirstOrDefaultAsync(c => c.Id == courtId);

        if (court is null)
        {
            return null;
        }

        // --- Thống kê đánh giá ---
        var reviewStats = await _context.Reviews
            .AsNoTracking()
            .Where(r => r.CourtId == courtId && r.IsVisible)
            .GroupBy(r => r.Rating)
            .Select(g => new { Rating = g.Key, Count = g.Count() })
            .ToListAsync();

        var totalReviews = reviewStats.Sum(x => x.Count);
        var averageRating = totalReviews > 0
            ? reviewStats.Sum(x => x.Rating * x.Count) / (double)totalReviews
            : 0;

        // Phân bố đánh giá theo số sao (5 -> 1), kể cả số sao chưa có đánh giá.
        var distribution = new Dictionary<int, int>();
        for (var star = 5; star >= 1; star--)
        {
            distribution[star] = reviewStats.FirstOrDefault(x => x.Rating == star)?.Count ?? 0;
        }

        // --- Danh sách đánh giá (phân trang) ---
        var reviews = await _context.Reviews
            .AsNoTracking()
            .Include(r => r.Customer)
            .Where(r => r.CourtId == courtId && r.IsVisible)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((reviewPage - 1) * reviewPageSize)
            .Take(reviewPageSize)
            .Select(r => new ReviewItemViewModel
            {
                Id = r.Id,
                Rating = r.Rating,
                Comment = r.Comment,
                CustomerName = r.Customer.FullName,
                CustomerAvatar = r.Customer.AvatarUrl,
                CreatedAt = r.CreatedAt,
                OwnerReply = r.OwnerReply,
                OwnerReplyAt = r.OwnerReplyAt
            })
            .ToListAsync();

        // --- Bảng giá theo khung giờ ---
        var timeSlotPrices = await _context.TimeSlots
            .AsNoTracking()
            .Where(t => t.CourtId == courtId && t.IsActive)
            .OrderBy(t => t.StartTime)
            .Select(t => new TimeSlotPriceViewModel
            {
                Id = t.Id,
                StartTime = t.StartTime,
                EndTime = t.EndTime,
                DurationMinutes = t.DurationMinutes,
                PriceWeekday = t.PriceWeekday,
                PriceWeekend = t.PriceWeekend,
                PriceHoliday = t.PriceHoliday
            })
            .ToListAsync();

        // --- Giá thấp nhất / cao nhất của sân ---
        var minPrice = timeSlotPrices.Count > 0 ? timeSlotPrices.Min(t => t.PriceWeekday) : 0;
        var maxPrice = timeSlotPrices.Count > 0 ? timeSlotPrices.Max(t => t.PriceWeekend) : 0;

        // --- Các sân khác cùng cụm (tối đa 4) ---
        var otherCourts = await _context.Courts
            .AsNoTracking()
            .Include(c => c.SportType)
            .Include(c => c.SportComplex)
            .Where(c => c.SportComplexId == court.SportComplexId
                        && c.Id != courtId
                        && c.Status == CourtStatus.Active)
            .OrderByDescending(c => c.Reviews.Where(r => r.IsVisible).Average(r => (double?)r.Rating) ?? 0)
            .Take(4)
            .Select(c => new CourtCardViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ImageUrl = c.ImageUrl,
                ComplexName = c.SportComplex.Name,
                Location = c.SportComplex.District + ", " + c.SportComplex.Province,
                SportTypeName = c.SportType.Name,
                SportTypeIcon = c.SportType.IconClass,
                MinPrice = c.TimeSlots.Where(t => t.IsActive).Min(t => (decimal?)t.PriceWeekday) ?? 0,
                MaxPrice = c.TimeSlots.Where(t => t.IsActive).Max(t => (decimal?)t.PriceWeekend) ?? 0,
                AverageRating = Math.Round(c.Reviews.Where(r => r.IsVisible).Average(r => (double?)r.Rating) ?? 0, 1),
                ReviewCount = c.Reviews.Count(r => r.IsVisible),
                Status = (int)c.Status
            })
            .ToListAsync();

        // --- Gallery: hiện tại chỉ có ảnh chính, có thể mở rộng ---
        var gallery = new List<string>();
        if (!string.IsNullOrWhiteSpace(court.ImageUrl))
        {
            gallery.Add(court.ImageUrl);
        }

        return new CourtDetailViewModel
        {
            Id = court.Id,
            Name = court.Name,
            Description = court.Description,
            ImageUrl = court.ImageUrl,
            Status = (int)court.Status,
            ComplexIsActive = court.SportComplex.IsActive,
            OwnerCanReceiveBookings = court.SportComplex.Owner.IsActive
                && court.SportComplex.Owner.ApprovalStatus == OwnerApprovalStatus.Approved,
            SportTypeIsActive = court.SportType.IsActive,
            SportComplexId = court.SportComplexId,
            ComplexName = court.SportComplex.Name,
            ComplexAddress = court.SportComplex.Address,
            Province = court.SportComplex.Province,
            District = court.SportComplex.District,
            ComplexDescription = court.SportComplex.Description,
            ComplexPhone = court.SportComplex.PhoneNumber,
            ComplexEmail = court.SportComplex.Email,
            OpeningTime = court.SportComplex.OpeningTime,
            ClosingTime = court.SportComplex.ClosingTime,
            SportTypeName = court.SportType.Name,
            SportTypeIcon = court.SportType.IconClass,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            AverageRating = Math.Round(averageRating, 1),
            ReviewCount = totalReviews,
            RatingDistribution = distribution,
            Reviews = reviews,
            TotalReviews = totalReviews,
            ReviewPage = reviewPage,
            ReviewPageSize = reviewPageSize,
            TimeSlotPrices = timeSlotPrices,
            GalleryImages = gallery,
            OtherCourtsInComplex = otherCourts
        };
    }

    // =========================================================================
    // 3. DỮ LIỆU TRANG CHỦ
    // =========================================================================
    public async Task<HomeViewModel> GetHomeDataAsync()
    {
        // --- Loại sân đang hoạt động kèm số lượng sân ---
        var sportTypes = await _context.SportTypes
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.DisplayOrder)
            .Select(t => new SportTypeCardViewModel
            {
                Id = t.Id,
                Name = t.Name,
                IconClass = t.IconClass,
                Description = t.Description,
                CourtCount = t.Courts.Count(c => c.Status == CourtStatus.Active)
            })
            .ToListAsync();

        // --- Sân nổi bật: điểm đánh giá cao, fallback sang sân mới nhất ---
        var featuredCourts = await _context.Courts
            .AsNoTracking()
            .Include(c => c.SportType)
            .Include(c => c.SportComplex)
            .Where(c => c.Status == CourtStatus.Active)
            .OrderByDescending(c => c.Reviews.Where(r => r.IsVisible).Average(r => (double?)r.Rating) ?? 0)
            .ThenByDescending(c => c.Reviews.Count(r => r.IsVisible))
            .Take(8)
            .Select(c => new CourtCardViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ImageUrl = c.ImageUrl,
                ComplexName = c.SportComplex.Name,
                Location = c.SportComplex.District + ", " + c.SportComplex.Province,
                SportTypeName = c.SportType.Name,
                SportTypeIcon = c.SportType.IconClass,
                MinPrice = c.TimeSlots.Where(t => t.IsActive).Min(t => (decimal?)t.PriceWeekday) ?? 0,
                MaxPrice = c.TimeSlots.Where(t => t.IsActive).Max(t => (decimal?)t.PriceWeekend) ?? 0,
                AverageRating = Math.Round(c.Reviews.Where(r => r.IsVisible).Average(r => (double?)r.Rating) ?? 0, 1),
                ReviewCount = c.Reviews.Count(r => r.IsVisible),
                Status = (int)c.Status
            })
            .ToListAsync();

        // --- Đánh giá nổi bật: 5 sao, có nhận xét ---
        var featuredReviews = await _context.Reviews
            .AsNoTracking()
            .Include(r => r.Customer)
            .Include(r => r.Court)
            .Where(r => r.IsVisible && r.Rating >= 4 && r.Comment != null && r.Comment != "")
            .OrderByDescending(r => r.Rating)
            .ThenByDescending(r => r.CreatedAt)
            .Take(6)
            .Select(r => new FeaturedReviewViewModel
            {
                Rating = r.Rating,
                Comment = r.Comment,
                CustomerName = r.Customer.FullName,
                CustomerAvatar = r.Customer.AvatarUrl,
                CourtName = r.Court.Name,
                CourtId = r.CourtId,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        // --- Thống kê tổng quan ---
        var stats = new HomeStatsViewModel
        {
            TotalCourts = await _context.Courts.CountAsync(c => c.Status == CourtStatus.Active),
            TotalBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Completed),
            TotalCustomers = await _context.Users.CountAsync(u => u.ApprovalStatus == OwnerApprovalStatus.NotApplicable),
            TotalOwners = await _context.Users.CountAsync(u => u.ApprovalStatus == OwnerApprovalStatus.Approved)
        };

        return new HomeViewModel
        {
            SportTypes = sportTypes,
            FeaturedCourts = featuredCourts,
            FeaturedReviews = featuredReviews,
            Stats = stats
        };
    }

    // =========================================================================
    // 4. DROPDOWN DATA
    // =========================================================================
    public async Task<List<SportTypeOption>> GetSportTypesAsync()
    {
        return await _context.SportTypes
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.DisplayOrder)
            .Select(t => new SportTypeOption
            {
                Id = t.Id,
                Name = t.Name,
                IconClass = t.IconClass
            })
            .ToListAsync();
    }

    public async Task<List<string>> GetProvincesAsync()
    {
        return await _context.SportComplexes
            .AsNoTracking()
            .Where(sc => sc.IsActive)
            .Select(sc => sc.Province)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync();
    }

    public async Task<List<string>> GetDistrictsAsync(string province)
    {
        if (string.IsNullOrWhiteSpace(province))
        {
            return new List<string>();
        }

        return await _context.SportComplexes
            .AsNoTracking()
            .Where(sc => sc.IsActive && sc.Province == province)
            .Select(sc => sc.District)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();
    }

    public async Task<List<TimeSlotOption>> GetTimeSlotOptionsAsync()
    {
        // Lấy các khung giờ bắt đầu phổ biến (distinct theo giờ bắt đầu),
        // giới hạn để dropdown không quá dài.
        var slots = await _context.TimeSlots
            .AsNoTracking()
            .Where(t => t.IsActive)
            .Select(t => new { t.StartTime, t.EndTime })
            .Distinct()
            .OrderBy(t => t.StartTime)
            .ToListAsync();

        return slots
            .GroupBy(t => t.StartTime)
            .Select(g => g.First())
            .Select(t => new TimeSlotOption
            {
                // Dùng giờ bắt đầu làm định danh hiển thị (TimeSlotId cụ thể phụ thuộc từng sân).
                Id = t.StartTime.Hours * 60 + t.StartTime.Minutes,
                DisplayText = $"{t.StartTime:hh\\:mm} - {t.EndTime:hh\\:mm}"
            })
            .Take(30)
            .ToList();
    }
}
