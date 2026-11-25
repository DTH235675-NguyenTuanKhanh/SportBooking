using Microsoft.EntityFrameworkCore;
using SportBooking.Data;
using SportBooking.Helpers;
using SportBooking.Models.Entities;
using SportBooking.Services.Interfaces;

namespace SportBooking.Services.Implementations;

public sealed class BankTransferService(ApplicationDbContext context)
{
    private const int ReviewMinutes = 15;

    private IQueryable<BankTransfer> Accessible(string? ownerId, string? customerId) =>
        context.BankTransfers.Where(t => (ownerId == null || t.OwnerId == ownerId)
            && (customerId == null || t.CustomerId == customerId));

    public Task<bool> ExistsAsync(string code, string? ownerId, string? customerId) =>
        Accessible(ownerId, customerId).AnyAsync(t => t.GroupCode == code);

    public Task<BankTransfer?> GetAsync(string code, string? ownerId, string? customerId) =>
        Accessible(ownerId, customerId).IgnoreQueryFilters().AsNoTracking().AsSplitQuery()
            .Include(t => t.Bookings).ThenInclude(b => b.Court)
            .Include(t => t.Bookings).ThenInclude(b => b.Customer)
            .Include(t => t.Bookings).ThenInclude(b => b.Payments)
            .Include(t => t.Reports)
            .SingleOrDefaultAsync(t => t.GroupCode == code);

    public Task<byte[]?> GetQrAsync(string code, string? ownerId, string? customerId) =>
        Accessible(ownerId, customerId).Where(t => t.GroupCode == code)
            .Select(t => t.QrImage.Png).SingleOrDefaultAsync();

    public Task<byte[]?> GetReceiptAsync(string code, int reportId, string? ownerId, string? customerId) =>
        Accessible(ownerId, customerId).Where(t => t.GroupCode == code)
            .SelectMany(t => t.Reports).Where(r => r.Id == reportId && r.ReceiptId != null)
            .Select(r => r.Receipt!.Png).SingleOrDefaultAsync();

    public async Task<ServiceResult> ReportAsync(string code, string customerId, IFormFile? file)
    {
        var receipt = file is not null ? await TransferImageUpload.ReadAsync(file) : null;
        if (file is not null && receipt is null) return ServiceResult.Fail(TransferImageUpload.Error);

        return await ChangeAsync(code, null, customerId, transfer =>
        {
            if (transfer.State == TransferState.Reported)
                return ServiceResult.Ok("Thông báo đã được gửi. Chờ chủ sân xác nhận.");
            if (transfer.State is not (TransferState.AwaitingReport or TransferState.NeedsInfo))
                return ServiceResult.Fail("Giao dịch này đã được chủ sân xử lý.");

            var now = ScheduleGuard.Now;
            var at = ScheduleGuard.UtcNow;
            var stillHeld = IsHeld(transfer, now);
            if (!stillHeld) Expire(transfer, at);
            if (stillHeld && transfer.LastReportedAt is null)
            {
                var deadline = ReviewDeadline(transfer, now);
                foreach (var booking in transfer.Bookings) booking.PaymentDeadline = deadline;
            }
            transfer.LastReportedAt = at;
            transfer.State = TransferState.Reported;
            if (receipt is not null) context.TransferImages.Add(receipt);
            transfer.Reports.Add(new TransferReport { ReportedAt = at, Receipt = receipt });
            return ServiceResult.Ok(stillHeld ? "Đã gửi thông báo. Chờ chủ sân xác nhận."
                : "Booking đã hết hạn. Đã gửi thông báo để chủ sân đối soát khoản chuyển.");
        });
    }

    public Task<ServiceResult> ReviewAsync(string code, int reportId, string ownerId, bool received, string? note)
    {
        var detail = note?.Trim();
        if (detail?.Length > 300 || (!received && string.IsNullOrEmpty(detail)))
            return Task.FromResult(ServiceResult.Fail("Nhập ghi chú xử lý từ 1 đến 300 ký tự."));

        return ChangeAsync(code, ownerId, null, transfer =>
        {
            var report = transfer.Reports.MaxBy(r => r.Id);
            if (report is null || report.Id != reportId)
                return ServiceResult.Fail("Đã có thông báo chuyển khoản khác. Hãy đọc lại trước khi xác nhận.");
            if (received && transfer.State is TransferState.Received or TransferState.ReceivedLate)
                return ServiceResult.Ok("Khoản chuyển này đã được ghi nhận.");
            if (transfer.State != TransferState.Reported)
                return ServiceResult.Fail("Chỉ xử lý khi có thông báo chuyển khoản đang chờ xác nhận.");

            var now = ScheduleGuard.Now;
            var at = ScheduleGuard.UtcNow;
            var stillHeld = IsHeld(transfer, now);
            if (!stillHeld) Expire(transfer, at);
            if (received)
            {
                foreach (var booking in transfer.Bookings)
                {
                    if (stillHeld)
                    {
                        booking.Status = BookingStatus.Confirmed;
                        booking.ConfirmedAt = at;
                    }
                    booking.PaymentStatus = stillHeld ? PaymentStatus.Paid : PaymentStatus.RefundPending;
                    booking.UpdatedAt = at;
                    var payment = booking.Payments.Single(p => p.Method == PaymentMethod.BankTransfer);
                    payment.Status = booking.PaymentStatus;
                    payment.PaidAt = at;
                    payment.UpdatedAt = at;
                }
            }
            transfer.State = !received ? TransferState.NeedsInfo
                : stillHeld ? TransferState.Received : TransferState.ReceivedLate;
            report.HandledById = ownerId;
            report.HandledAt = at;
            report.Outcome = transfer.State;
            report.HandlingNote = detail;
            return ServiceResult.Ok(transfer.State switch
            {
                TransferState.Received => "Đã xác nhận nhận tiền và xác nhận lịch đặt.",
                TransferState.ReceivedLate => "Đã ghi nhận tiền đến sau hạn. Đơn chuyển sang chờ hoàn thủ công.",
                _ => "Đã ghi nhận chưa tìm thấy giao dịch hoặc cần bổ sung thông tin."
            });
        });
    }

    public Task<ServiceResult> ConfirmRefundAsync(string code, int bookingId, string ownerId, string? note)
    {
        var detail = note?.Trim();
        if (string.IsNullOrEmpty(detail) || detail.Length > 300)
            return Task.FromResult(ServiceResult.Fail("Nhập ghi chú hoàn tiền từ 1 đến 300 ký tự."));

        return ChangeAsync(code, ownerId, null, transfer =>
        {
            var booking = transfer.Bookings.SingleOrDefault(b => b.Id == bookingId);
            if (booking is null) return ServiceResult.Fail("Không tìm thấy lượt đặt trong nhóm.");
            if (booking.PaymentStatus == PaymentStatus.Refunded) return ServiceResult.Ok("Khoản tiền đã được ghi nhận hoàn.");
            if (booking.Status is not (BookingStatus.Cancelled or BookingStatus.Expired)
                || booking.PaymentStatus != PaymentStatus.RefundPending)
                return ServiceResult.Fail("Lượt đặt này không có khoản chuyển đang chờ hoàn.");

            var at = ScheduleGuard.UtcNow;
            var payment = booking.Payments.Single(p => p.Method == PaymentMethod.BankTransfer);
            payment.ManualRefundedAt = at;
            payment.ManualRefundedById = ownerId;
            payment.ManualRefundNote = detail;
            payment.Status = PaymentStatus.Refunded;
            payment.UpdatedAt = at;
            booking.PaymentStatus = PaymentStatus.Refunded;
            booking.UpdatedAt = at;
            return ServiceResult.Ok("Đã ghi nhận hoàn tiền thủ công.");
        });
    }

    public async Task ExpireDueAsync(CancellationToken token)
    {
        var now = ScheduleGuard.Now;
        var due = await context.Bookings.AsNoTracking().Where(b => b.BankTransferGroupCode != null
                && b.Status == BookingStatus.Pending && b.PaymentDeadline <= now)
            .Select(b => b.BankTransferGroupCode!).Distinct().ToListAsync(token);

        foreach (var code in due)
        {
            token.ThrowIfCancellationRequested();
            await ChangeAsync(code, null, null, transfer =>
            {
                if (!IsHeld(transfer, ScheduleGuard.Now)) Expire(transfer, ScheduleGuard.UtcNow);
                return ServiceResult.Ok();
            });
        }
    }

    private async Task<ServiceResult> ChangeAsync(string code, string? ownerId, string? customerId,
        Func<BankTransfer, ServiceResult> change)
    {
        var query = Accessible(ownerId, customerId).Where(t => t.GroupCode == code);
        var complexId = await query.Select(t => (int?)t.ComplexId).SingleOrDefaultAsync();
        if (complexId is null) return ServiceResult.Fail("Không tìm thấy đơn chuyển khoản.");

        return await context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            context.ChangeTracker.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync();
            await ScheduleGuard.LockAsync(context, complexId.Value);
            var transfer = await query.AsSplitQuery().Include(t => t.Bookings).ThenInclude(b => b.Payments)
                .Include(t => t.Reports).SingleOrDefaultAsync();
            if (transfer is null || transfer.Bookings.Count == 0)
                return ServiceResult.Fail("Không tìm thấy đơn chuyển khoản.");

            var result = change(transfer);
            if (result.Success)
            {
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            return result;
        });
    }

    private static bool IsHeld(BankTransfer transfer, DateTime now) =>
        transfer.Bookings.All(b => b.Status == BookingStatus.Pending && b.PaymentDeadline > now && b.StartDateTime > now);

    private static DateTime ReviewDeadline(BankTransfer transfer, DateTime now)
    {
        var until = now.AddMinutes(ReviewMinutes);
        var firstStart = transfer.Bookings.Min(b => b.StartDateTime);
        return until < firstStart ? until : firstStart;
    }

    private static void Expire(BankTransfer transfer, DateTime at)
    {
        foreach (var booking in transfer.Bookings.Where(b => b.Status == BookingStatus.Pending))
        {
            booking.Status = BookingStatus.Expired;
            booking.PaymentStatus = PaymentStatus.Failed;
            booking.CancelledAt = at;
            booking.CancelledBy = "System";
            booking.CancellationReason = "Booking đã hết hạn giữ chỗ chuyển khoản.";
            booking.UpdatedAt = at;
            var payment = booking.Payments.Single(p => p.Method == PaymentMethod.BankTransfer);
            payment.Status = PaymentStatus.Failed;
            payment.UpdatedAt = at;
        }
    }
}
