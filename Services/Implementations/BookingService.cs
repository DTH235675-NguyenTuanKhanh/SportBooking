// File: Services/Implementations/BookingService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SportBooking.Constants;
using SportBooking.Data;
using SportBooking.Helpers;
using SportBooking.Models.Entities;
using SportBooking.Models.ViewModels.Booking;
using SportBooking.Models.ViewModels.Shared;
using SportBooking.Services.Interfaces;

namespace SportBooking.Services.Implementations;

/// <summary>
/// Nghiệp vụ đặt sân.
/// Điểm quan trọng nhất: chống đặt trùng khung giờ bằng 2 lớp bảo vệ:
///   1. Kiểm tra trong transaction (lớp ứng dụng).
///   2. Unique filtered index ở database (lớp cuối, chống race condition).
/// </summary>
public class BookingService : IBookingService
{
    private readonly ApplicationDbContext _context;
    private readonly BookingSettings _bookingSettings;
    private readonly ILogger<BookingService> _logger;

    /// <summary>Các trạng thái được coi là "đang giữ chỗ" của một khung giờ.</summary>
    private static readonly BookingStatus[] HoldingStatuses =
    {
        BookingStatus.Pending,
        BookingStatus.Confirmed,
        BookingStatus.Completed
    };

    public BookingService(
        ApplicationDbContext context,
        IOptions<BookingSettings> bookingSettings,
        ILogger<BookingService> logger)
    {
        _context = context;
        _bookingSettings = bookingSettings.Value;
        _logger = logger;
    }

    // =========================================================================
    // 1. CHUẨN BỊ FORM ĐẶT SÂN
    // =========================================================================
    public async Task<ServiceResult<BookingCreateViewModel>> PrepareBookingFormAsync(int courtId, DateTime? bookingDate = null)
    {
        var court = await _context.Courts
            .AsNoTracking()
            .Include(c => c.SportType)
            .Include(c => c.SportComplex)
                .ThenInclude(sc => sc.Owner)
            .FirstOrDefaultAsync(c => c.Id == courtId);

        if (court is null)
        {
            return ServiceResult<BookingCreateViewModel>.Fail("Không tìm thấy sân.");
        }

        if (!IsCourtBookable(court))
        {
            return ServiceResult<BookingCreateViewModel>.Fail("Sân này hiện không nhận đặt lịch.");
        }

        // Mặc định đặt cho ngày mai nếu không truyền ngày.
        var targetDate = (bookingDate ?? SportBooking.Helpers.ScheduleGuard.Now.Date.AddDays(1)).Date;

        // Kiểm tra ngày nằm trong khoảng cho phép.
        var (isValidDate, dateError) = ValidateBookingDate(targetDate);
        if (!isValidDate)
        {
            return ServiceResult<BookingCreateViewModel>.Fail(dateError!);
        }

        // Lấy trạng thái khung giờ.
        var slotResult = await GetTimeSlotAvailabilityAsync(courtId, targetDate);
        if (!slotResult.Success)
        {
            return ServiceResult<BookingCreateViewModel>.Fail(slotResult.Message);
        }

        var model = new BookingCreateViewModel
        {
            CourtId = court.Id,
            CourtName = court.Name,
            ComplexName = court.SportComplex.Name,
            ComplexAddress = $"{court.SportComplex.Address}, {court.SportComplex.District}, {court.SportComplex.Province}",
            SportTypeName = court.SportType.Name,
            CourtImageUrl = court.ImageUrl,
            BookingDate = targetDate,
            TimeSlots = slotResult.Data!,
            TransferAvailable = await _context.OwnerTransferProfiles.AnyAsync(p => p.OwnerId == court.SportComplex.OwnerId && p.Enabled)
        };

        return ServiceResult<BookingCreateViewModel>.Ok(model);
    }

    // =========================================================================
    // 2. LẤY TRẠNG THÁI KHUNG GIỜ (AJAX)
    // =========================================================================
    public async Task<ServiceResult<List<TimeSlotAvailabilityViewModel>>> GetTimeSlotAvailabilityAsync(int courtId, DateTime bookingDate)
    {
        var targetDate = bookingDate.Date;

        var court = await _context.Courts
            .AsNoTracking()
            .Include(c => c.SportComplex).ThenInclude(sc => sc.Owner)
            .Include(c => c.SportType)
            .FirstOrDefaultAsync(c => c.Id == courtId);

        if (court is null)
        {
            return ServiceResult<List<TimeSlotAvailabilityViewModel>>.Fail("Không tìm thấy sân.");
        }

        // Lấy tất cả khung giờ đang hoạt động của sân.
        var slots = await _context.TimeSlots
            .AsNoTracking()
            .Where(t => t.CourtId == courtId && t.IsActive)
            .OrderBy(t => t.StartTime)
            .ToListAsync();

        if (slots.Count == 0)
        {
            return ServiceResult<List<TimeSlotAvailabilityViewModel>>.Ok(
                new List<TimeSlotAvailabilityViewModel>(),
                "Sân này chưa được thiết lập khung giờ.");
        }

        // Xác định loại ngày để chọn đúng bảng giá.
        var dayType = ResolveDayType(targetDate);

        // Dùng giờ đã chốt trên Booking: sau khi đổi giờ TimeSlot, ID khung
        // không còn đủ để xác định một khoảng thời gian đang bị giữ chỗ.
        var dateOnly = DateOnly.FromDateTime(targetDate);
        var reservedIntervals = await _context.Bookings
            .AsNoTracking()
            .Where(b => b.CourtId == courtId
                        && b.BookingDate == dateOnly
                        && HoldingStatuses.Contains(b.Status))
            .Select(b => new { b.StartDateTime, b.EndDateTime })
            .ToListAsync();

        var isMaintenance = !IsCourtBookable(court);
        var now = SportBooking.Helpers.ScheduleGuard.Now;

        // Map sang ViewModel với trạng thái tương ứng.
        var result = slots.Select(slot =>
        {
            var slotStart = targetDate.Add(slot.StartTime);

            TimeSlotStatus status;

            if (isMaintenance || (court.SportComplex.OpeningTime.HasValue
                && (slot.StartTime < court.SportComplex.OpeningTime || slot.EndTime > court.SportComplex.ClosingTime)))
            {
                // Sân bảo trì: tất cả khung giờ đều không đặt được.
                status = TimeSlotStatus.Maintenance;
            }
            else if (slotStart <= now)
            {
                // Khung giờ đã trôi qua so với hiện tại.
                status = TimeSlotStatus.Past;
            }
            else if (reservedIntervals.Any(b => b.StartDateTime < targetDate.Add(slot.EndTime)
                && b.EndDateTime > slotStart))
            {
                status = TimeSlotStatus.Booked;
            }
            else
            {
                status = TimeSlotStatus.Available;
            }

            return new TimeSlotAvailabilityViewModel
            {
                TimeSlotId = slot.Id,
                Quote = SportBooking.Helpers.SlotQuote.Create(slot, targetDate),
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                DurationMinutes = slot.DurationMinutes,
                Price = slot.GetPriceByDayType(dayType),
                Status = status
            };
        }).ToList();

        return ServiceResult<List<TimeSlotAvailabilityViewModel>>.Ok(result);
    }

    /// <summary>
    /// Xác định loại ngày (ngày thường / cuối tuần / ngày lễ) để tra bảng giá.
    /// Hiện tại chưa có bảng ngày lễ riêng nên chỉ phân biệt ngày thường và cuối tuần.
    /// </summary>
    private static DayType ResolveDayType(DateTime date)
    {
        return date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
            ? DayType.Weekend
            : DayType.Weekday;
    }

    /// <summary>
    /// Kiểm tra ngày đặt sân có hợp lệ (không ở quá khứ, không vượt quá số ngày cho phép).
    /// </summary>
    private (bool IsValid, string? Error) ValidateBookingDate(DateTime date)
    {
        if (date.Date < SportBooking.Helpers.ScheduleGuard.Now.Date)
        {
            return (false, "Không thể đặt sân cho ngày đã qua.");
        }

        var maxDate = SportBooking.Helpers.ScheduleGuard.Now.Date.AddDays(_bookingSettings.MaxAdvanceBookingDays);
        if (date.Date > maxDate)
        {
            return (false, $"Chỉ có thể đặt sân trước tối đa {_bookingSettings.MaxAdvanceBookingDays} ngày.");
        }

        return (true, null);
    }

    // =========================================================================
    // 3. TẠO BOOKING (CHỐNG ĐẶT TRÙNG)
    // =========================================================================
    public async Task<ServiceResult<BookingCreatedResult>> CreateBookingAsync(string customerId, BookingCreateViewModel model)
    {
        if (model.PaymentMethod is not ("BankTransfer" or "Counter"))
        {
            return ServiceResult<BookingCreatedResult>.Fail("Phương thức thanh toán không hợp lệ.");
        }
        // ---------- 3.1 Validate đầu vào ----------
        if (model.SelectedTimeSlotIds is null || model.SelectedTimeSlotIds.Count == 0)
        {
            return ServiceResult<BookingCreatedResult>.Fail("Vui lòng chọn ít nhất một khung giờ.");
        }

        if (model.SelectedTimeSlotIds.Count > _bookingSettings.MaxSlotsPerBooking)
        {
            return ServiceResult<BookingCreatedResult>.Fail(
                $"Chỉ được đặt tối đa {_bookingSettings.MaxSlotsPerBooking} khung giờ trong một lần.");
        }

        if (!_bookingSettings.AllowMultipleConsecutiveSlots && model.SelectedTimeSlotIds.Count > 1)
        {
            return ServiceResult<BookingCreatedResult>.Fail("Hệ thống hiện chỉ cho phép đặt một khung giờ mỗi lần.");
        }

        var (isValidDate, dateError) = ValidateBookingDate(model.BookingDate);
        if (!isValidDate)
        {
            return ServiceResult<BookingCreatedResult>.Fail(dateError!);
        }

        var targetDate = model.BookingDate.Date;
        var dateOnly = DateOnly.FromDateTime(targetDate);

        // ---------- 3.2 Lấy thông tin sân ----------
        var court = await _context.Courts
            .Include(c => c.SportComplex)
                .ThenInclude(sc => sc.Owner)
            .Include(c => c.SportType)
            .FirstOrDefaultAsync(c => c.Id == model.CourtId);

        if (court is null)
        {
            return ServiceResult<BookingCreatedResult>.Fail("Không tìm thấy sân.");
        }

        if (!IsCourtBookable(court))
        {
            return ServiceResult<BookingCreatedResult>.Fail("Sân này hiện không nhận đặt lịch.");
        }

        // ---------- 3.3 Lấy các khung giờ được chọn ----------
        var slots = await _context.TimeSlots
            .Where(t => model.SelectedTimeSlotIds.Contains(t.Id) && t.CourtId == model.CourtId && t.IsActive)
            .OrderBy(t => t.StartTime)
            .ToListAsync();

        if (slots.Count != model.SelectedTimeSlotIds.Count)
        {
            return ServiceResult<BookingCreatedResult>.Fail(
                "Một số khung giờ bạn chọn không hợp lệ hoặc không thuộc sân này.");
        }

        if (slots.Zip(slots.Skip(1)).Any(pair => pair.First.EndTime != pair.Second.StartTime))
            return ServiceResult<BookingCreatedResult>.Fail("Vui lòng chọn các khung giờ liền nhau trên cùng sân.");

        // ---------- 3.4 Kiểm tra khung giờ đã qua ----------
        var now = SportBooking.Helpers.ScheduleGuard.Now;
        var pastSlots = slots.Where(s => targetDate.Add(s.StartTime) <= now).ToList();
        if (pastSlots.Count > 0)
        {
            var names = string.Join(", ", pastSlots.Select(s => $"{s.StartTime:hh\\:mm}"));
            return ServiceResult<BookingCreatedResult>.Fail(
                $"Các khung giờ sau đã trôi qua và không thể đặt: {names}.");
        }

        // ---------- 3.5 Kiểm tra trùng lịch (lớp ứng dụng) ----------
        var requestedSlotIds = slots.Select(s => s.Id).ToList();
        var conflictingSlotIds = await _context.Bookings
            .AsNoTracking()
            .Where(b => b.CourtId == model.CourtId
                        && b.BookingDate == dateOnly
                        && requestedSlotIds.Contains(b.TimeSlotId)
                        && HoldingStatuses.Contains(b.Status))
            .Select(b => b.TimeSlotId)
            .ToListAsync();

        if (conflictingSlotIds.Count > 0)
        {
            var conflicting = slots.Where(s => conflictingSlotIds.Contains(s.Id)).ToList();
            var names = string.Join(", ", conflicting.Select(s => $"{s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm}"));

            _logger.LogWarning("Phát hiện đặt trùng: sân {CourtId}, ngày {Date}, khung giờ {Slots}.",
                model.CourtId, targetDate.ToString("yyyy-MM-dd"), names);

            return ServiceResult<BookingCreatedResult>.Fail(
                $"Rất tiếc, các khung giờ sau vừa được người khác đặt: {names}. " +
                "Vui lòng chọn khung giờ khác.");
        }

        // ---------- 3.6 Tính tiền ----------
        var dayType = ResolveDayType(targetDate);
        var totalAmount = slots.Sum(s => s.GetPriceByDayType(dayType));

        // ---------- 3.7 Xác định phương thức thanh toán ----------
        var isTransfer = model.PaymentMethod == "BankTransfer";
        var paymentMethod = isTransfer ? PaymentMethod.BankTransfer : PaymentMethod.AtCounter;

        // ---------- 3.8 Sinh mã booking ----------
        var groupCode = "GRP" + Guid.NewGuid().ToString("N")[..24];

        // ---------- 3.9 Tạo các bản ghi booking trong transaction ----------
        // Dùng transaction để đảm bảo tính nguyên tử: hoặc tạo hết, hoặc không tạo gì.
        // Bọc trong ExecutionStrategy vì Program.cs bật EnableRetryOnFailure:
        // SqlServerRetryingExecutionStrategy không hỗ trợ transaction mở thủ công, và toàn bộ
        // giao dịch phải được chạy lại như một đơn vị duy nhất khi có lỗi tạm thời.
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            _context.ChangeTracker.Clear();
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                await SportBooking.Helpers.ScheduleGuard.LockAsync(_context, court.SportComplexId);
                var currentCourt = await _context.Courts.AsNoTracking().Include(c => c.SportComplex)
                    .ThenInclude(c => c.Owner).Include(c => c.SportType).SingleOrDefaultAsync(c => c.Id == model.CourtId);
                if (currentCourt is null || !IsCourtBookable(currentCourt))
                    return ServiceResult<BookingCreatedResult>.Fail("Sân này hiện không nhận đặt lịch.");
                // Re-read under the same lock used by Owner schedule changes.
                slots = await _context.TimeSlots.AsNoTracking().Where(t => requestedSlotIds.Contains(t.Id)
                    && t.CourtId == court.Id && t.IsActive).OrderBy(t => t.StartTime).ToListAsync();
                var currentNow = SportBooking.Helpers.ScheduleGuard.Now;
                if (slots.Count != requestedSlotIds.Count || slots.Any(t => t.StartTime < TimeSpan.Zero
                    || t.EndTime >= TimeSpan.FromDays(1) || t.StartTime >= t.EndTime
                    || targetDate.Add(t.StartTime) <= currentNow
                    || (currentCourt.SportComplex.OpeningTime.HasValue
                        && (t.StartTime < currentCourt.SportComplex.OpeningTime || t.EndTime > currentCourt.SportComplex.ClosingTime))))
                    return ServiceResult<BookingCreatedResult>.Fail("Khung giờ đã thay đổi hoặc nằm ngoài giờ mở cửa. Vui lòng chọn lại.");
                if (slots.Zip(slots.Skip(1)).Any(pair => pair.First.EndTime != pair.Second.StartTime))
                    return ServiceResult<BookingCreatedResult>.Fail("Vui lòng chọn các khung giờ liền nhau.");
                totalAmount = slots.Sum(s => s.GetPriceByDayType(dayType));
                if (slots.Any(s => !model.SlotQuotes.TryGetValue(s.Id, out var quote)
                    || quote != SportBooking.Helpers.SlotQuote.Create(s, targetDate)))
                    return ServiceResult<BookingCreatedResult>.Fail("Giờ hoặc giá khung đã thay đổi. Vui lòng kiểm tra lịch mới và bấm đặt lại để xác nhận.");
                if (slots.Any(s => s.GetPriceByDayType(dayType) <= 0) || model.TotalAmount != totalAmount)
                    return ServiceResult<BookingCreatedResult>.Fail("Giá khung giờ đã thay đổi. Vui lòng kiểm tra tổng tiền mới và bấm đặt lại để xác nhận.");
                if (await _context.Bookings.AsNoTracking().AnyAsync(b => b.CourtId == court.Id && b.BookingDate == dateOnly
                    && HoldingStatuses.Contains(b.Status) && b.StartDateTime < targetDate.Add(slots.Last().EndTime)
                    && b.EndDateTime > targetDate.Add(slots.First().StartTime)))
                    return ServiceResult<BookingCreatedResult>.Fail("Khung giờ vừa được người khác đặt. Vui lòng chọn lại.");
                var bookings = new List<Booking>();
                var sequence = 1;
                BankTransfer? transfer = null;
                if (isTransfer)
                {
                    var profile = await _context.OwnerTransferProfiles.AsNoTracking()
                        .SingleOrDefaultAsync(p => p.OwnerId == currentCourt.SportComplex.OwnerId && p.Enabled);
                    if (profile is null)
                        return ServiceResult<BookingCreatedResult>.Fail("Chủ sân chưa bật chuyển khoản qua QR. Vui lòng chọn thanh toán tại quầy.");

                    var deadline = currentNow.AddMinutes(_bookingSettings.PaymentTimeoutMinutes);
                    var firstStart = targetDate.Add(slots[0].StartTime);
                    transfer = new BankTransfer
                    {
                        GroupCode = groupCode, Reference = "SB" + groupCode[3..].ToUpperInvariant(),
                        OwnerId = profile.OwnerId, CustomerId = customerId, ComplexId = currentCourt.SportComplexId,
                        BankName = profile.BankName, AccountNumber = profile.AccountNumber, AccountName = profile.AccountName,
                        QrImageId = profile.QrImageId, Amount = totalAmount,
                        ReportDeadline = deadline < firstStart ? deadline : firstStart
                    };
                }

                foreach (var slot in slots)
                {
                    var booking = new Booking
                    {
                        // Mã nhóm có 96 bit ngẫu nhiên, mã từng khung giờ nằm trong giới hạn 30 ký tự.
                        BookingCode = $"{groupCode}-{sequence++}",
                        BookingGroupCode = groupCode,
                        BankTransfer = transfer,
                        CustomerId = customerId,
                        CourtId = court.Id,
                        TimeSlotId = slot.Id,
                        BookingDate = dateOnly,
                        StartDateTime = targetDate.Add(slot.StartTime),
                        EndDateTime = targetDate.Add(slot.EndTime),
                        TotalAmount = slot.GetPriceByDayType(dayType),
                        Status = BookingStatus.Pending,
                        PaymentStatus = PaymentStatus.Unpaid,
                        PaymentMethod = paymentMethod,
                        Note = model.Notes?.Trim(),
                        CreatedAt = ScheduleGuard.UtcNow,
                        PaymentDeadline = transfer?.ReportDeadline
                    };
                    if (transfer is not null)
                    {
                        booking.Payments.Add(new Payment
                        {
                            TransactionCode = $"{transfer.Reference}-{sequence - 1}",
                            Method = PaymentMethod.BankTransfer, Amount = booking.TotalAmount,
                            CreatedAt = booking.CreatedAt
                        });
                    }

                    bookings.Add(booking);
                }

                await _context.Bookings.AddRangeAsync(bookings);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Tạo booking thành công: nhóm {GroupCode}, {Count} khung giờ, tổng tiền {Amount}.",
                    groupCode, bookings.Count, totalAmount);

                var result = new BookingCreatedResult
                {
                    BookingGroupCode = groupCode,
                    BookingCodes = bookings.Select(b => b.BookingCode).ToList(),
                    TotalAmount = totalAmount,
                    PaymentMethod = model.PaymentMethod,
                    FirstBookingId = bookings.First().Id
                };

                return ServiceResult<BookingCreatedResult>.Ok(result, "Đặt sân thành công!");
            }
            catch (DbUpdateException ex) when (IsSlotConflict(ex))
            {
                // ---------- Bắt lỗi vi phạm unique index (race condition) ----------
                // Trường hợp 2 request đồng thời vượt qua bước kiểm tra ở lớp ứng dụng:
                // unique filtered index ở database sẽ chặn request đến sau.
                await transaction.RollbackAsync();

                _logger.LogWarning(ex,
                    "Đặt trùng bị database chặn: sân {CourtId}, ngày {Date}, khách {CustomerId}.",
                    model.CourtId, targetDate.ToString("yyyy-MM-dd"), customerId);

                return ServiceResult<BookingCreatedResult>.Fail(
                    "Rất tiếc, khung giờ bạn chọn vừa được người khác đặt. Vui lòng chọn khung giờ khác.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(ex, "Lỗi khi tạo booking cho khách {CustomerId}.", customerId);

                return ServiceResult<BookingCreatedResult>.Fail(
                    "Có lỗi xảy ra khi đặt sân. Vui lòng thử lại sau.");
            }
        });
    }

    /// <summary>
    /// Kiểm tra exception có phải do vi phạm ràng buộc duy nhất (unique index) hay không.
    /// SQL Server trả về error number 2601 (duplicate key in unique index)
    /// hoặc 2627 (violation of unique constraint).
    /// </summary>
    private static bool IsSlotConflict(DbUpdateException ex)
    {
        var sqlException = ex.InnerException as Microsoft.Data.SqlClient.SqlException;
        if (sqlException is null)
        {
            return false;
        }

        return (sqlException.Number is 2601 or 2627)
            && sqlException.Message.Contains("UX_Bookings_Court_Slot_Date_Active", StringComparison.Ordinal);
    }

    private static bool IsCourtBookable(Court court) => court.Status == CourtStatus.Active
        && court.SportComplex.IsActive && court.SportComplex.Owner.IsActive
        && court.SportComplex.Owner.ApprovalStatus == OwnerApprovalStatus.Approved
        && court.SportType.IsActive;

    // =========================================================================
    // 4. DANH SÁCH BOOKING CỦA KHÁCH HÀNG
    // =========================================================================
    public async Task<BookingListViewModel> GetCustomerBookingsAsync(string customerId, BookingListViewModel filter)
    {
        if (filter.Page < 1)
        {
            filter.Page = 1;
        }

        if (filter.PageSize < 1 || filter.PageSize > 50)
        {
            filter.PageSize = 10;
        }

        var query = _context.Bookings.IgnoreQueryFilters()
            .AsNoTracking()
            .Include(b => b.Court)
                .ThenInclude(c => c.SportComplex)
            .Include(b => b.Review)
            .Where(b => b.CustomerId == customerId);

        // --- Lọc theo trạng thái ---
        if (!string.IsNullOrWhiteSpace(filter.StatusFilter) && filter.StatusFilter != "all")
        {
            var statusFilter = filter.StatusFilter switch
            {
                "pending" => BookingStatus.Pending,
                "confirmed" => BookingStatus.Confirmed,
                "completed" => BookingStatus.Completed,
                "cancelled" => BookingStatus.Cancelled,
                "expired" => BookingStatus.Expired,
                "noshow" => BookingStatus.NoShow,
                _ => (BookingStatus?)null
            };

            if (statusFilter.HasValue)
            {
                query = query.Where(b => b.Status == statusFilter.Value);
            }
        }

        // --- Lọc theo khoảng thời gian ---
        if (filter.FromDate.HasValue)
        {
            var from = DateOnly.FromDateTime(filter.FromDate.Value.Date);
            query = query.Where(b => b.BookingDate >= from);
        }

        if (filter.ToDate.HasValue)
        {
            var to = DateOnly.FromDateTime(filter.ToDate.Value.Date);
            query = query.Where(b => b.BookingDate <= to);
        }

        // --- Tìm theo mã booking hoặc tên sân ---
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim();
            query = query.Where(b =>
                b.BookingCode.Contains(keyword) ||
                b.BookingGroupCode.Contains(keyword) ||
                b.Court.Name.Contains(keyword) ||
                b.Court.SportComplex.Name.Contains(keyword));
        }

        // --- Thống kê số lượng theo trạng thái (cho tab badges) ---
        var allStatusCounts = await _context.Bookings.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(b => b.CustomerId == customerId)
            .GroupBy(b => b.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        filter.StatusCounts["all"] = allStatusCounts.Sum(x => x.Count);
        filter.StatusCounts["pending"] = allStatusCounts.FirstOrDefault(x => x.Status == BookingStatus.Pending)?.Count ?? 0;
        filter.StatusCounts["confirmed"] = allStatusCounts.FirstOrDefault(x => x.Status == BookingStatus.Confirmed)?.Count ?? 0;
        filter.StatusCounts["completed"] = allStatusCounts.FirstOrDefault(x => x.Status == BookingStatus.Completed)?.Count ?? 0;
        filter.StatusCounts["cancelled"] = allStatusCounts.FirstOrDefault(x => x.Status == BookingStatus.Cancelled)?.Count ?? 0;
        filter.StatusCounts["expired"] = allStatusCounts.FirstOrDefault(x => x.Status == BookingStatus.Expired)?.Count ?? 0;
        filter.StatusCounts["noshow"] = allStatusCounts.FirstOrDefault(x => x.Status == BookingStatus.NoShow)?.Count ?? 0;

        // --- Đếm tổng kết quả ---
        filter.TotalCount = await query.CountAsync();

        // --- Lấy dữ liệu trang hiện tại ---
        var bookings = await query
            .OrderByDescending(b => b.BookingDate)
            .ThenByDescending(b => b.StartDateTime)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(b => new
            {
                b.Id,
                b.BookingCode,
                b.BookingGroupCode,
                b.BankTransferGroupCode,
                b.CourtId,
                CourtName = b.Court.Name,
                CourtImageUrl = b.Court.ImageUrl,
                ComplexName = b.Court.SportComplex.Name,
                ComplexAddress = b.Court.SportComplex.Address,
                b.BookingDate,
                StartTime = b.StartDateTime.TimeOfDay,
                EndTime = b.EndDateTime.TimeOfDay,
                b.TotalAmount,
                b.Status,
                b.PaymentStatus,
                b.PaymentMethod,
                b.CreatedAt,
                b.PaymentDeadline,
                b.StartDateTime,
                HasReview = b.Review != null
            })
            .ToListAsync();

        var now = SportBooking.Helpers.ScheduleGuard.Now;
        var minCancelHours = _bookingSettings.CancellationMinHoursBefore;

        filter.Bookings = bookings.Select(b =>
        {
            // Điều kiện hủy: trạng thái Confirmed và còn đủ số giờ tối thiểu trước giờ bắt đầu.
            var canCancel = b.Status == BookingStatus.Confirmed
                            && (b.StartDateTime - now).TotalHours >= minCancelHours;

            string? cancelDisabledReason = null;
            if (b.Status == BookingStatus.Confirmed && !canCancel)
            {
                cancelDisabledReason =
                    $"Chỉ có thể hủy trước giờ bắt đầu tối thiểu {minCancelHours} giờ.";
            }
            else if (b.Status != BookingStatus.Confirmed)
            {
                cancelDisabledReason = "Chỉ có thể hủy booking ở trạng thái \"Đã xác nhận\".";
            }

            return new BookingListItemViewModel
            {
                Id = b.Id,
                BookingCode = b.BookingCode,
                BookingGroupCode = b.BookingGroupCode,
                BankTransferGroupCode = b.BankTransferGroupCode,
                CourtId = b.CourtId,
                CourtName = b.CourtName,
                CourtImageUrl = b.CourtImageUrl,
                ComplexName = b.ComplexName,
                ComplexAddress = b.ComplexAddress,
                BookingDate = b.BookingDate.ToDateTime(TimeOnly.MinValue),
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                TotalAmount = b.TotalAmount,
                Status = (int)b.Status,
                PaymentStatus = (int)b.PaymentStatus,
                PaymentMethodText = MapPaymentMethod(b.PaymentMethod),
                StatusText = MapBookingStatusText(b.Status),
                StatusBadgeClass = MapBookingStatusBadge(b.Status),
                PaymentStatusText = MapPaymentStatusText(b.PaymentStatus),
                CreatedAt = b.CreatedAt,
                PaymentDeadline = b.PaymentDeadline,
                CanCancel = canCancel,
                CancelDisabledReason = cancelDisabledReason,
                // Chỉ đánh giá được booking đã hoàn thành và chưa có đánh giá.
                CanReview = b.Status == BookingStatus.Completed && !b.HasReview,
                HasReview = b.HasReview
            };
        }).ToList();

        return filter;
    }

    // =========================================================================
    // 5. CHI TIẾT BOOKING
    // =========================================================================
    public async Task<ServiceResult<BookingDetailViewModel>> GetBookingDetailAsync(int bookingId, string userId, bool isOwnerOrAdmin)
    {
        var booking = await _context.Bookings.IgnoreQueryFilters()
            .AsNoTracking()
            .Include(b => b.Court)
                .ThenInclude(c => c.SportComplex)
                    .ThenInclude(sc => sc.Owner)
            .Include(b => b.Court)
                .ThenInclude(c => c.SportType)
            .Include(b => b.TimeSlot)
            .Include(b => b.Customer)
            .Include(b => b.Payments)
            .Include(b => b.Review)
            .FirstOrDefaultAsync(b => b.Id == bookingId
                && (b.CustomerId == userId || (isOwnerOrAdmin && b.Court.SportComplex.OwnerId == userId)));

        if (booking is null)
        {
            return ServiceResult<BookingDetailViewModel>.Fail("Không tìm thấy booking.");
        }

        // ---------- Kiểm tra quyền sở hữu ----------
        // Khách hàng chỉ xem được booking của mình; Owner chỉ xem booking thuộc sân của mình.
        if (!isOwnerOrAdmin && booking.CustomerId != userId)
        {
            _logger.LogWarning("User {UserId} cố truy cập booking {BookingId} không thuộc sở hữu.",
                userId, bookingId);
            return ServiceResult<BookingDetailViewModel>.Fail("Bạn không có quyền xem booking này.");
        }

        var now = SportBooking.Helpers.ScheduleGuard.Now;
        var minCancelHours = _bookingSettings.CancellationMinHoursBefore;

        var canCancel = booking.Status == BookingStatus.Confirmed
                        && booking.CustomerId == userId
                        && (booking.StartDateTime - now).TotalHours >= minCancelHours;

        string? cancelDisabledReason = null;
        if (booking.CustomerId == userId && !canCancel)
        {
            cancelDisabledReason = booking.Status switch
            {
                BookingStatus.Confirmed => $"Chỉ có thể hủy trước giờ bắt đầu tối thiểu {minCancelHours} giờ.",
                BookingStatus.Pending => "Booking chưa được xác nhận.",
                BookingStatus.Completed => "Booking đã hoàn thành.",
                BookingStatus.Cancelled => "Booking đã bị hủy.",
                BookingStatus.Expired => "Booking đã hết hạn.",
                BookingStatus.NoShow => "Booking đã đánh dấu vắng mặt.",
                _ => "Không thể hủy booking này."
            };
        }

        var model = new BookingDetailViewModel
        {
            Id = booking.Id,
            BookingCode = booking.BookingCode,
            BookingGroupCode = booking.BookingGroupCode,
            BankTransferGroupCode = booking.BankTransferGroupCode,
            CourtId = booking.CourtId,
            CourtName = booking.Court.Name,
            CourtImageUrl = booking.Court.ImageUrl,
            ComplexName = booking.Court.SportComplex.Name,
            ComplexAddress = $"{booking.Court.SportComplex.Address}, {booking.Court.SportComplex.District}, {booking.Court.SportComplex.Province}",
            ComplexPhone = booking.Court.SportComplex.PhoneNumber ?? string.Empty,
            SportTypeName = booking.Court.SportType.Name,
            BookingDate = booking.BookingDate.ToDateTime(TimeOnly.MinValue),
            StartTime = booking.StartDateTime.TimeOfDay,
            EndTime = booking.EndDateTime.TimeOfDay,
            StartDateTime = booking.StartDateTime,
            EndDateTime = booking.EndDateTime,
            TotalAmount = booking.TotalAmount,
            Status = (int)booking.Status,
            StatusText = MapBookingStatusText(booking.Status),
            StatusBadgeClass = MapBookingStatusBadge(booking.Status),
            PaymentStatus = (int)booking.PaymentStatus,
            PaymentStatusText = MapPaymentStatusText(booking.PaymentStatus),
            PaymentMethodText = MapPaymentMethod(booking.PaymentMethod),
            Notes = booking.Note,
            CancellationReason = booking.CancellationReason,
            CancelledAt = booking.CancelledAt,
            CancelledBy = booking.CancelledBy,
            ConfirmedAt = booking.ConfirmedAt,
            CompletedAt = booking.CompletedAt,
            PaymentDeadline = booking.PaymentDeadline,
            CreatedAt = booking.CreatedAt,
            CustomerName = booking.Customer.FullName,
            CustomerPhone = booking.Customer.PhoneNumber ?? string.Empty,
            CustomerEmail = booking.Customer.Email ?? string.Empty,
            CanCancel = canCancel,
            CancelDisabledReason = cancelDisabledReason,
            CanReview = booking.Status == BookingStatus.Completed
                        && booking.CustomerId == userId
                        && booking.Review is null,
            Payments = booking.Payments
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PaymentInfoViewModel
                {
                    Id = p.Id,
                    TransactionCode = p.TransactionCode,
                    MethodText = MapPaymentMethod(p.Method),
                    Amount = p.Amount,
                    StatusText = MapPaymentStatusText(p.Status),
                    StatusBadgeClass = MapPaymentStatusBadge(p.Status),
                    BankCode = p.BankCode,
                    ResponseMessage = p.ResponseMessage,
                    PaidAt = p.PaidAt,
                    CreatedAt = p.CreatedAt
                })
                .ToList()
        };

        if (booking.Review is not null)
        {
            model.Review = new ReviewInfoViewModel
            {
                Id = booking.Review.Id,
                Rating = booking.Review.Rating,
                Comment = booking.Review.Comment,
                OwnerReply = booking.Review.OwnerReply,
                OwnerReplyAt = booking.Review.OwnerReplyAt,
                CreatedAt = booking.Review.CreatedAt
            };
        }

        return ServiceResult<BookingDetailViewModel>.Ok(model);
    }

    // =========================================================================
    // 6. HỦY BOOKING
    // =========================================================================
    public async Task<(bool CanCancel, string? Reason)> CanCancelBookingAsync(int bookingId, string customerId)
    {
        var booking = await _context.Bookings.IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.CustomerId == customerId);

        if (booking is null)
        {
            return (false, "Không tìm thấy booking.");
        }

        if (booking.Status != BookingStatus.Confirmed)
        {
            return (false, "Chỉ có thể hủy booking ở trạng thái \"Đã xác nhận\".");
        }

        var hoursUntilStart = (booking.StartDateTime - SportBooking.Helpers.ScheduleGuard.Now).TotalHours;
        if (hoursUntilStart < _bookingSettings.CancellationMinHoursBefore)
        {
            return (false,
                $"Chỉ có thể hủy trước giờ bắt đầu tối thiểu {_bookingSettings.CancellationMinHoursBefore} giờ.");
        }

        return (true, null);
    }

    public async Task<ServiceResult<BookingCancelViewModel>> PrepareCancelFormAsync(int bookingId, string customerId)
    {
        var booking = await _context.Bookings.IgnoreQueryFilters()
            .AsNoTracking()
            .Include(b => b.Court)
                .ThenInclude(c => c.SportComplex)
            .Include(b => b.TimeSlot)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.CustomerId == customerId);

        if (booking is null)
        {
            return ServiceResult<BookingCancelViewModel>.Fail("Không tìm thấy booking.");
        }

        var (canCancel, reason) = await CanCancelBookingAsync(bookingId, customerId);
        if (!canCancel)
        {
            return ServiceResult<BookingCancelViewModel>.Fail(reason!);
        }

        var model = new BookingCancelViewModel
        {
            BookingId = booking.Id,
            BookingCode = booking.BookingCode,
            CourtName = booking.Court.Name,
            ComplexName = booking.Court.SportComplex.Name,
            BookingDate = booking.BookingDate.ToDateTime(TimeOnly.MinValue),
            StartTime = booking.StartDateTime.TimeOfDay,
            EndTime = booking.EndDateTime.TimeOfDay,
            TotalAmount = booking.TotalAmount,
            PaymentStatus = (int)booking.PaymentStatus,
            MinHoursBefore = _bookingSettings.CancellationMinHoursBefore
        };

        return ServiceResult<BookingCancelViewModel>.Ok(model);
    }

    public async Task<ServiceResult> CancelBookingAsync(int bookingId, string customerId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 10)
        {
            return ServiceResult.Fail("Vui lòng nhập lý do hủy (tối thiểu 10 ký tự).");
        }

        var booking = await _context.Bookings.IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.CustomerId == customerId);

        if (booking is null)
        {
            return ServiceResult.Fail("Không tìm thấy booking.");
        }

        // Kiểm tra lại điều kiện hủy ngay trước khi cập nhật (tránh trường hợp vừa hết hạn).
        var (canCancel, cancelReason) = await CanCancelBookingAsync(bookingId, customerId);
        if (!canCancel)
        {
            return ServiceResult.Fail(cancelReason!);
        }

        booking.Status = BookingStatus.Cancelled;
        booking.CancellationReason = reason.Trim();
        booking.CancelledAt = DateTime.UtcNow;
        booking.CancelledBy = "Customer";
        booking.UpdatedAt = DateTime.UtcNow;

        // Ghi nhận yêu cầu hoàn tiền; chưa được coi là đã trả tiền cho khách.
        if (booking.PaymentStatus == PaymentStatus.Paid)
        {
            booking.PaymentStatus = PaymentStatus.RefundPending;

            // Cập nhật trạng thái các giao dịch liên quan.
            var payments = await _context.Payments
                .Where(p => p.BookingId == booking.Id && p.Status == PaymentStatus.Paid)
                .ToListAsync();

            foreach (var payment in payments)
            {
                payment.Status = PaymentStatus.RefundPending;
                payment.UpdatedAt = DateTime.UtcNow;
            }
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult.Fail("Trạng thái lịch đặt vừa thay đổi. Vui lòng tải lại trang.");
        }

        _logger.LogInformation("Khách {CustomerId} đã hủy booking {BookingCode}. Lý do: {Reason}",
            customerId, booking.BookingCode, reason);

        return ServiceResult.Ok(
            "Hủy đặt sân thành công. " +
            (booking.PaymentStatus == PaymentStatus.RefundPending
                ? "Khoản thanh toán đang chờ hoàn tiền. Vui lòng liên hệ chủ sân để xử lý."
                : ""));
    }

    // =========================================================================
    // 7. ĐÁNH GIÁ
    // =========================================================================
    public async Task<ServiceResult<ReviewCreateViewModel>> PrepareReviewFormAsync(int bookingId, string customerId)
    {
        var booking = await _context.Bookings.IgnoreQueryFilters()
            .AsNoTracking()
            .Include(b => b.Court)
            .Include(b => b.TimeSlot)
            .Include(b => b.Review)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.CustomerId == customerId);

        if (booking is null)
        {
            return ServiceResult<ReviewCreateViewModel>.Fail("Không tìm thấy booking.");
        }

        if (booking.Status != BookingStatus.Completed)
        {
            return ServiceResult<ReviewCreateViewModel>.Fail(
                "Chỉ có thể đánh giá sân sau khi booking đã hoàn thành.");
        }

        if (booking.Review is not null)
        {
            return ServiceResult<ReviewCreateViewModel>.Fail(
                "Bạn đã đánh giá booking này rồi. Mỗi booking chỉ được đánh giá một lần.");
        }

        var model = new ReviewCreateViewModel
        {
            BookingId = booking.Id,
            BookingCode = booking.BookingCode,
            CourtName = booking.Court.Name,
            CourtImageUrl = booking.Court.ImageUrl,
            BookingDate = booking.BookingDate.ToDateTime(TimeOnly.MinValue),
            StartTime = booking.StartDateTime.TimeOfDay,
            EndTime = booking.EndDateTime.TimeOfDay
        };

        return ServiceResult<ReviewCreateViewModel>.Ok(model);
    }

    public async Task<ServiceResult> SubmitReviewAsync(string customerId, ReviewCreateViewModel model)
    {
        var booking = await _context.Bookings.IgnoreQueryFilters()
            .Include(b => b.Review)
            .FirstOrDefaultAsync(b => b.Id == model.BookingId && b.CustomerId == customerId);

        if (booking is null)
        {
            return ServiceResult.Fail("Không tìm thấy booking.");
        }

        // Kiểm tra nghiệp vụ: chỉ được đánh giá booking đã hoàn thành và chưa đánh giá.
        if (booking.Status != BookingStatus.Completed)
        {
            return ServiceResult.Fail("Chỉ có thể đánh giá sân sau khi booking đã hoàn thành.");
        }

        if (booking.Review is not null)
        {
            return ServiceResult.Fail("Bạn đã đánh giá booking này rồi.");
        }

        if (model.Rating < 1 || model.Rating > 5)
        {
            return ServiceResult.Fail("Số sao đánh giá phải từ 1 đến 5.");
        }

        var review = new Review
        {
            BookingId = booking.Id,
            CourtId = booking.CourtId,
            CustomerId = customerId,
            Rating = model.Rating,
            Comment = model.Comment?.Trim(),
            IsVisible = true,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Reviews.AddAsync(review);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Khách {CustomerId} đã đánh giá {Rating} sao cho booking {BookingCode}.",
            customerId, model.Rating, booking.BookingCode);

        return ServiceResult.Ok("Cảm ơn bạn đã đánh giá! Đánh giá của bạn đã được ghi nhận.");
    }

    // =========================================================================
    // HELPER: MAP TRẠNG THÁI SANG TIẾNG VIỆT + CSS CLASS
    // =========================================================================
    /// <summary>Chuyển trạng thái booking sang text tiếng Việt.</summary>
    public static string MapBookingStatusText(BookingStatus status) => status switch
    {
        BookingStatus.Pending => "Chờ xác nhận",
        BookingStatus.Confirmed => "Đã xác nhận",
        BookingStatus.Completed => "Hoàn thành",
        BookingStatus.Cancelled => "Đã hủy",
        BookingStatus.Expired => "Hết hạn",
        BookingStatus.NoShow => "Vắng mặt",
        _ => "Không xác định"
    };

    /// <summary>Chuyển trạng thái booking sang CSS class của badge.</summary>
    public static string MapBookingStatusBadge(BookingStatus status) => status switch
    {
        BookingStatus.Pending => "badge-pending",
        BookingStatus.Confirmed => "badge-confirmed",
        BookingStatus.Completed => "badge-completed",
        BookingStatus.Cancelled => "badge-cancelled",
        BookingStatus.Expired => "badge-expired",
        BookingStatus.NoShow => "badge-noshow",
        _ => "badge-noshow"
    };

    /// <summary>Chuyển trạng thái thanh toán sang text tiếng Việt.</summary>
    public static string MapPaymentStatusText(PaymentStatus status) => status switch
    {
        PaymentStatus.Unpaid => "Chưa thanh toán",
        PaymentStatus.Paid => "Đã thanh toán",
        PaymentStatus.Refunded => "Đã hoàn tiền",
        PaymentStatus.RefundPending => "Chờ hoàn tiền",
        PaymentStatus.Failed => "Thanh toán thất bại",
        _ => "Không xác định"
    };

    /// <summary>CSS class cho badge trạng thái thanh toán.</summary>
    public static string MapPaymentStatusBadge(PaymentStatus status) => status switch
    {
        PaymentStatus.Paid => "badge-completed",
        PaymentStatus.Refunded => "badge-confirmed",
        PaymentStatus.RefundPending => "badge-pending",
        PaymentStatus.Failed => "badge-cancelled",
        _ => "badge-pending"
    };

    /// <summary>Chuyển phương thức thanh toán sang text tiếng Việt.</summary>
    public static string MapPaymentMethod(PaymentMethod method) => method switch
    {
        PaymentMethod.VnPay => "VNPay",
        PaymentMethod.BankTransfer => "Chuyển khoản qua QR",
        PaymentMethod.MoMo => "MoMo",
        PaymentMethod.AtCounter => "Thanh toán tại quầy",
        _ => "Không xác định"
    };
}
