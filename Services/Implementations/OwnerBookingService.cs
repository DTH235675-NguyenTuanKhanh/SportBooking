using Microsoft.EntityFrameworkCore;
using SportBooking.Data;
using SportBooking.Helpers;
using SportBooking.Models.Entities;
using SportBooking.Models.ViewModels.Owner;
using SportBooking.Services.Interfaces;

namespace SportBooking.Services.Implementations;

/// <summary>
/// Nghiệp vụ xem và cập nhật booking trong phạm vi cụm sân của Chủ sân.
/// </summary>
public class OwnerBookingService : IOwnerBookingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OwnerBookingService> _logger;

    public OwnerBookingService(
        ApplicationDbContext context,
        ILogger<OwnerBookingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<OwnerBookingListViewModel> GetBookingsAsync(string ownerId, OwnerBookingListViewModel filter)
    {
        filter.Page = Math.Max(1, filter.Page);
        filter.PageSize = filter.PageSize is < 1 or > 50 ? 20 : filter.PageSize;

        var query = _context.Bookings.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(b => b.Court.SportComplex.OwnerId == ownerId);

        if (filter.TransferReview)
            query = query.Where(b => b.BankTransfer != null && b.BankTransfer.State == TransferState.Reported);

        if (!string.IsNullOrWhiteSpace(filter.StatusFilter)
            && Enum.TryParse<BookingStatus>(filter.StatusFilter, ignoreCase: true, out var status))
        {
            query = query.Where(b => b.Status == status);
        }

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

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim();
            query = query.Where(b => b.BookingCode.Contains(keyword)
                                     || b.BookingGroupCode.Contains(keyword)
                                     || b.Customer.FullName.Contains(keyword)
                                     || (b.Customer.PhoneNumber != null && b.Customer.PhoneNumber.Contains(keyword))
                                     || b.Court.Name.Contains(keyword)
                                     || b.Court.SportComplex.Name.Contains(keyword));
        }

        filter.TotalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(filter.TotalCount / (double)filter.PageSize);
        if (totalPages > 0 && filter.Page > totalPages)
        {
            filter.Page = totalPages;
        }

        filter.Bookings = await query
            .OrderByDescending(b => b.BookingDate)
            .ThenBy(b => b.StartDateTime)
            .ThenBy(b => b.Id)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(b => new OwnerBookingItemViewModel
            {
                Id = b.Id,
                BookingCode = b.BookingCode,
                BookingGroupCode = b.BookingGroupCode,
                BankTransferGroupCode = b.BankTransferGroupCode,
                TransferState = b.BankTransfer == null ? null : b.BankTransfer.State,
                CustomerName = b.Customer.FullName,
                CustomerPhone = b.Customer.PhoneNumber ?? string.Empty,
                CourtName = b.Court.Name,
                ComplexName = b.Court.SportComplex.Name,
                BookingDate = b.BookingDate.ToDateTime(TimeOnly.MinValue),
                StartDateTime = b.StartDateTime,
                EndDateTime = b.EndDateTime,
                StartTime = b.StartDateTime.TimeOfDay,
                EndTime = b.EndDateTime.TimeOfDay,
                TotalAmount = b.TotalAmount,
                Status = b.Status,
                PaymentStatus = b.PaymentStatus,
                PaymentMethod = b.PaymentMethod,
                Note = b.Note,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync();

        return filter;
    }

    public async Task<ServiceResult> ConfirmCounterPaymentAsync(string ownerId, string bookingGroupCode)
    {
        if (string.IsNullOrWhiteSpace(bookingGroupCode))
        {
            return ServiceResult.Fail("Mã nhóm đặt sân không hợp lệ.");
        }

        var now = SportBooking.Helpers.ScheduleGuard.Now;
        var group = await _context.Bookings.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(b => b.BookingGroupCode == bookingGroupCode
                        && b.Court.SportComplex.OwnerId == ownerId)
            .Select(b => new { b.CourtId, b.BookingDate })
            .Distinct()
            .ToListAsync();

        if (group.Count != 1)
        {
            return ServiceResult.Fail("Không tìm thấy nhóm đặt sân thuộc quyền quản lý của bạn.");
        }

        var groupCourtId = group[0].CourtId;
        var groupDate = group[0].BookingDate;
        var bookings = await _context.Bookings.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(b => b.BookingGroupCode == bookingGroupCode
                        && b.CourtId == groupCourtId
                        && b.BookingDate == groupDate
                        && b.Court.SportComplex.OwnerId == ownerId)
            .OrderBy(b => b.StartDateTime)
            .Select(b => new
            {
                b.Id,
                b.BookingCode,
                b.TotalAmount,
                b.StartDateTime,
                b.Status,
                b.PaymentStatus,
                b.PaymentMethod
            })
            .ToListAsync();

        if (bookings.Count == 0)
        {
            return ServiceResult.Fail("Không tìm thấy nhóm đặt sân thuộc quyền quản lý của bạn.");
        }

        if (bookings.Any(b => b.StartDateTime <= now))
        {
            return ServiceResult.Fail("Không thể xác nhận thanh toán tại quầy sau giờ bắt đầu sân.");
        }

        if (bookings.Any(b => b.Status != BookingStatus.Pending
                              || b.PaymentStatus != PaymentStatus.Unpaid
                              || b.PaymentMethod != PaymentMethod.AtCounter))
        {
            return ServiceResult.Fail("Nhóm đặt sân không còn ở trạng thái chờ xác nhận thanh toán tại quầy.");
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        try
        {
            return await strategy.ExecuteAsync(async () =>
            {
                var confirmedAt = ScheduleGuard.Now;
                await using var transaction = await _context.Database.BeginTransactionAsync();

                var updatedCount = await _context.Bookings.IgnoreQueryFilters()
                    .Where(b => b.BookingGroupCode == bookingGroupCode
                                && b.CourtId == groupCourtId
                                && b.BookingDate == groupDate
                                && b.Court.SportComplex.OwnerId == ownerId
                                && b.Status == BookingStatus.Pending
                                && b.PaymentStatus == PaymentStatus.Unpaid
                                && b.PaymentMethod == PaymentMethod.AtCounter
                                && b.StartDateTime > confirmedAt)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(b => b.Status, BookingStatus.Confirmed)
                        .SetProperty(b => b.PaymentStatus, PaymentStatus.Paid)
                        .SetProperty(b => b.ConfirmedAt, confirmedAt)
                        .SetProperty(b => b.UpdatedAt, confirmedAt));

                if (updatedCount != bookings.Count)
                {
                    await transaction.RollbackAsync();
                    return ServiceResult.Fail("Trạng thái nhóm đặt sân vừa thay đổi. Hãy tải lại danh sách và kiểm tra.");
                }

                var payments = bookings.Select(booking => new Payment
                {
                    BookingId = booking.Id,
                    TransactionCode = $"CTR-{Guid.NewGuid():N}",
                    Method = PaymentMethod.AtCounter,
                    Amount = booking.TotalAmount,
                    Status = PaymentStatus.Paid,
                    ResponseCode = "00",
                    ResponseMessage = "Chủ sân xác nhận đã thu tiền tại quầy.",
                    PaidAt = confirmedAt,
                    CreatedAt = confirmedAt
                });

                await _context.Payments.AddRangeAsync(payments);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Chủ sân {OwnerId} xác nhận thanh toán nhóm {GroupCode} gồm {Count} booking.",
                    ownerId, bookingGroupCode, updatedCount);

                return ServiceResult.Ok($"Đã xác nhận thanh toán tại quầy cho {updatedCount} khung giờ.");
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xác nhận thanh toán tại quầy nhóm {GroupCode} của Owner {OwnerId}.",
                bookingGroupCode, ownerId);
            return ServiceResult.Fail("Có lỗi xảy ra khi xác nhận thanh toán. Vui lòng thử lại.");
        }
    }

    public async Task<ServiceResult> ConfirmCounterRefundAsync(string ownerId, int bookingId)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var now = ScheduleGuard.UtcNow;
            var updated = await _context.Bookings.IgnoreQueryFilters()
                .Where(b => b.Id == bookingId && b.Court.SportComplex.OwnerId == ownerId
                            && b.Status == BookingStatus.Cancelled
                            && b.PaymentMethod == PaymentMethod.AtCounter
                            && b.PaymentStatus == PaymentStatus.RefundPending)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(b => b.PaymentStatus, PaymentStatus.Refunded)
                    .SetProperty(b => b.UpdatedAt, now));
            if (updated != 1)
                return ServiceResult.Fail("Không tìm thấy khoản chờ hoàn tiền tại quầy thuộc quyền quản lý của bạn.");

            await _context.Payments
                .Where(p => p.BookingId == bookingId && p.Method == PaymentMethod.AtCounter
                            && p.Status == PaymentStatus.RefundPending)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.Status, PaymentStatus.Refunded)
                    .SetProperty(p => p.ResponseMessage, PaymentRevenue.CounterRefundMessage)
                    .SetProperty(p => p.ManualRefundedAt, now)
                    .SetProperty(p => p.ManualRefundedById, ownerId)
                    .SetProperty(p => p.UpdatedAt, now));
            await transaction.CommitAsync();
            return ServiceResult.Ok("Đã xác nhận hoàn tiền tại quầy cho khách hàng.");
        });
    }

    public async Task<ServiceResult> UpdateBookingStatusAsync(string ownerId, int bookingId, BookingStatus targetStatus)
    {
        if (bookingId <= 0)
        {
            return ServiceResult.Fail("Booking không hợp lệ.");
        }

        var now = SportBooking.Helpers.ScheduleGuard.Now;
        var booking = await _context.Bookings.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(b => b.Id == bookingId && b.Court.SportComplex.OwnerId == ownerId)
            .Select(b => new { b.Id, b.BookingCode, b.Status, b.PaymentStatus, b.EndDateTime, b.StartDateTime })
            .FirstOrDefaultAsync();

        if (booking is null)
        {
            return ServiceResult.Fail("Không tìm thấy booking thuộc quyền quản lý của bạn.");
        }

        if (targetStatus is not (BookingStatus.Completed or BookingStatus.NoShow))
        {
            return ServiceResult.Fail("Trạng thái cập nhật không hợp lệ.");
        }

        if (booking.Status != BookingStatus.Confirmed || booking.PaymentStatus != PaymentStatus.Paid)
        {
            return ServiceResult.Fail("Chỉ cập nhật được booking đã xác nhận và thanh toán.");
        }

        if (targetStatus == BookingStatus.Completed && booking.EndDateTime > now)
        {
            return ServiceResult.Fail("Chỉ đánh dấu hoàn thành sau khi khung giờ kết thúc.");
        }

        if (targetStatus == BookingStatus.NoShow && booking.StartDateTime > now)
        {
            return ServiceResult.Fail("Chỉ đánh dấu vắng mặt từ giờ bắt đầu trở đi.");
        }

        var eligibleBookings = _context.Bookings.IgnoreQueryFilters()
            .Where(b => b.Id == bookingId
                        && b.Court.SportComplex.OwnerId == ownerId
                        && b.Status == BookingStatus.Confirmed
                        && b.PaymentStatus == PaymentStatus.Paid
                        && (targetStatus == BookingStatus.Completed
                            ? b.EndDateTime <= now
                            : b.StartDateTime <= now));

        var updatedCount = targetStatus == BookingStatus.Completed
            ? await eligibleBookings.ExecuteUpdateAsync(setters => setters
                .SetProperty(b => b.Status, BookingStatus.Completed)
                .SetProperty(b => b.CompletedAt, now)
                .SetProperty(b => b.UpdatedAt, now))
            : await eligibleBookings.ExecuteUpdateAsync(setters => setters
                .SetProperty(b => b.Status, BookingStatus.NoShow)
                .SetProperty(b => b.UpdatedAt, now));

        if (updatedCount != 1)
        {
            return ServiceResult.Fail("Trạng thái booking vừa thay đổi. Hãy tải lại danh sách trước khi thao tác.");
        }

        _logger.LogInformation("Chủ sân {OwnerId} cập nhật booking {BookingCode} sang {Status}.",
            ownerId, booking.BookingCode, targetStatus);

        return ServiceResult.Ok(targetStatus == BookingStatus.Completed
            ? "Đã cập nhật booking thành hoàn thành."
            : "Đã cập nhật booking thành vắng mặt.");
    }
}
