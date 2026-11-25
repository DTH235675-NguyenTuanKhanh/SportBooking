using Microsoft.EntityFrameworkCore;
using SportBooking.Data;
using SportBooking.Models.Entities;
using SportBooking.Services.Interfaces;

namespace SportBooking.Services.Implementations;

/// <summary>
/// Chuyển booking Pending đã quá hạn sang Expired để giải phóng khung giờ.
/// </summary>
public class BookingExpirationService : IBookingExpirationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BookingExpirationService> _logger;
    private readonly BankTransferService _transfers;

    public BookingExpirationService(
        ApplicationDbContext context,
        ILogger<BookingExpirationService> logger,
        BankTransferService transfers)
    {
        _context = context;
        _logger = logger;
        _transfers = transfers;
    }

    public async Task<int> ExpirePendingBookingsAsync(CancellationToken cancellationToken = default)
    {
        await _transfers.ExpireDueAsync(cancellationToken);
        var now = SportBooking.Helpers.ScheduleGuard.Now;
        var strategy = _context.Database.CreateExecutionStrategy();

        var counts = await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var onlineIds = await _context.Bookings
                .Where(b => b.Status == BookingStatus.Pending
                            && b.PaymentStatus != PaymentStatus.Paid
                            && (b.PaymentMethod == PaymentMethod.VnPay || b.PaymentMethod == PaymentMethod.MoMo)
                            && _context.Bookings.Any(expired =>
                                expired.BookingGroupCode == b.BookingGroupCode
                                && expired.CustomerId == b.CustomerId
                                && expired.CourtId == b.CourtId
                                && expired.BookingDate == b.BookingDate
                                && expired.Status == BookingStatus.Pending
                                && expired.PaymentMethod == b.PaymentMethod
                                && expired.PaymentStatus != PaymentStatus.Paid
                                && expired.PaymentDeadline.HasValue
                                && expired.PaymentDeadline.Value <= now))
                .Select(b => b.Id)
                .ToListAsync(cancellationToken);

            var expiredOnline = onlineIds.Count == 0
                ? 0
                : await _context.Bookings
                    .Where(b => onlineIds.Contains(b.Id)
                                && b.Status == BookingStatus.Pending
                                && b.PaymentStatus != PaymentStatus.Paid
                                && (b.PaymentMethod == PaymentMethod.VnPay || b.PaymentMethod == PaymentMethod.MoMo))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(b => b.Status, BookingStatus.Expired)
                        .SetProperty(b => b.PaymentStatus, PaymentStatus.Failed)
                        .SetProperty(b => b.CancelledAt, now)
                        .SetProperty(b => b.CancelledBy, "System")
                        .SetProperty(b => b.CancellationReason, "Quá hạn thanh toán online.")
                        .SetProperty(b => b.UpdatedAt, now), cancellationToken);

            if (expiredOnline > 0)
            {
                await _context.Payments
                    .Where(p => onlineIds.Contains(p.BookingId)
                                && p.Status == PaymentStatus.Unpaid
                                && p.Booking.Status == BookingStatus.Expired
                                && p.Booking.CancelledBy == "System")
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.Status, PaymentStatus.Failed)
                        .SetProperty(p => p.ResponseCode, "EXPIRED")
                        .SetProperty(p => p.ResponseMessage, "Booking hết hạn trước khi thanh toán thành công.")
                        .SetProperty(p => p.UpdatedAt, now), cancellationToken);
            }

            // Đặt tại quầy là một nhóm xác nhận chung; khi khung đầu tiên bắt đầu mà nhóm
            // vẫn Pending thì hết hạn toàn nhóm, thay vì giữ các khung sau ở trạng thái Pending.
            var counterIds = await _context.Bookings
                .Where(b => b.Status == BookingStatus.Pending
                            && b.PaymentMethod == PaymentMethod.AtCounter
                            && _context.Bookings.Any(expired =>
                                expired.BookingGroupCode == b.BookingGroupCode
                                && expired.CustomerId == b.CustomerId
                                && expired.CourtId == b.CourtId
                                && expired.BookingDate == b.BookingDate
                                && expired.Status == BookingStatus.Pending
                                && expired.PaymentMethod == PaymentMethod.AtCounter
                                && expired.StartDateTime <= now))
                .Select(b => b.Id)
                .ToListAsync(cancellationToken);

            var expiredAtCounter = counterIds.Count == 0
                ? 0
                : await _context.Bookings
                    .Where(b => counterIds.Contains(b.Id)
                                && b.Status == BookingStatus.Pending
                                && b.PaymentMethod == PaymentMethod.AtCounter)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(b => b.Status, BookingStatus.Expired)
                        .SetProperty(b => b.CancelledAt, now)
                        .SetProperty(b => b.CancelledBy, "System")
                        .SetProperty(b => b.CancellationReason, "Quá giờ bắt đầu nhưng chưa được Chủ sân xác nhận.")
                        .SetProperty(b => b.UpdatedAt, now), cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return (expiredOnline, expiredAtCounter);
        });

        var totalExpired = counts.expiredOnline + counts.expiredAtCounter;
        if (totalExpired > 0)
        {
            _logger.LogInformation(
                "Đã hết hạn {Count} booking Pending (online: {OnlineCount}, tại quầy: {CounterCount}).",
                totalExpired, counts.expiredOnline, counts.expiredAtCounter);
        }

        return totalExpired;
    }
}
