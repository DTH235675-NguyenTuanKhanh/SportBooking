using Microsoft.EntityFrameworkCore;
using SportBooking.Data;
using SportBooking.Helpers;
using SportBooking.Models.Entities;
using SportBooking.Models.ViewModels.Owner;
using SportBooking.Services.Interfaces;

namespace SportBooking.Services.Implementations;

public sealed class OwnerRevenueService(ApplicationDbContext context) : IOwnerRevenueService
{
    public async Task<OwnerRevenueViewModel> ReadAsync(string ownerId, OwnerRevenueFilter filter, bool validInput,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(ScheduleGuard.Now);
        var complexOptions = await context.SportComplexes.AsNoTracking()
            .Where(c => c.OwnerId == ownerId).OrderBy(c => c.Name)
            .Select(c => new RevenueOption(c.Id, c.Name)).ToListAsync(cancellationToken);
        var courtOptions = await context.Courts.IgnoreQueryFilters().AsNoTracking()
            .Where(c => c.SportComplex.OwnerId == ownerId
                && (filter.ComplexId == null || c.SportComplexId == filter.ComplexId))
            .OrderBy(c => c.SportComplex.Name).ThenBy(c => c.Name)
            .Select(c => new RevenueOption(c.Id, c.SportComplex.Name + " · " + c.Name
                + (c.IsDeleted ? " (đã xóa mềm)" : ""))).ToListAsync(cancellationToken);
        var page = new OwnerRevenueViewModel { Filter = filter, Complexes = complexOptions, Courts = courtOptions };
        filter.From ??= new DateOnly(today.Year, today.Month, 1);
        filter.To ??= today;
        if (!validInput || filter.To < filter.From || filter.To == DateOnly.MaxValue
            || filter.To.Value.DayNumber - filter.From.Value.DayNumber >= 366)
        {
            page.Error = "Chọn khoảng ngày hợp lệ, tối đa 366 ngày.";
            return page;
        }
        if ((filter.ComplexId is { } complex && !complexOptions.Any(c => c.Id == complex))
            || (filter.CourtId is { } court && !courtOptions.Any(c => c.Id == court)))
        {
            page.Error = "Sân hoặc cụm sân đã chọn không thuộc phạm vi báo cáo.";
            return page;
        }

        var bookings = context.Bookings.IgnoreQueryFilters().AsNoTracking()
            .Where(b => b.Court.SportComplex.OwnerId == ownerId
                && (filter.ComplexId == null || b.Court.SportComplexId == filter.ComplexId)
                && (filter.CourtId == null || b.CourtId == filter.CourtId));
        var payments = bookings.SelectMany(b => b.Payments);
        page.Days = await PaymentRevenue.ByDayAsync(payments, filter.From.Value, filter.To.Value, cancellationToken);
        page.UndatedPayments = await PaymentRevenue.Posted(payments).CountAsync(p =>
            p.PaidAt == null || (p.Status == PaymentStatus.Refunded && p.ManualRefundedAt == null
                && (p.Method != PaymentMethod.AtCounter || p.UpdatedAt == null
                    || p.ResponseMessage != PaymentRevenue.CounterRefundMessage)), cancellationToken);
        page.BookingsWithoutPayment = await bookings.CountAsync(b =>
            (b.PaymentStatus == PaymentStatus.Paid || b.PaymentStatus == PaymentStatus.Refunded
                || b.PaymentStatus == PaymentStatus.RefundPending)
            && !b.Payments.Any(p => p.Status == PaymentStatus.Paid || p.Status == PaymentStatus.Refunded
                || p.Status == PaymentStatus.RefundPending), cancellationToken);
        return page;
    }
}

internal static class PaymentRevenue
{
    internal const string CounterRefundMessage = "Chủ sân xác nhận đã trả lại tiền tại quầy.";

    internal static IQueryable<Payment> Posted(IQueryable<Payment> payments) => payments.Where(p =>
        p.Status == PaymentStatus.Paid || p.Status == PaymentStatus.RefundPending || p.Status == PaymentStatus.Refunded);

    internal static Task<List<RevenueDay>> ByDayAsync(IQueryable<Payment> payments, DateOnly from, DateOnly to,
        CancellationToken cancellationToken)
    {
        var lower = from.ToDateTime(TimeOnly.MinValue);
        var upper = to.AddDays(1).ToDateTime(TimeOnly.MinValue);

        // Counter receipts and historical VNP fixtures were written in Vietnam local time.
        // Real gateway, QR receipts and manual refund audit timestamps use UTC.
        var receipts = Posted(payments).Where(p => p.PaidAt != null).Select(p => new
        {
            LocalAt = p.Method == PaymentMethod.AtCounter
                || (p.Method == PaymentMethod.VnPay && p.TransactionCode.StartsWith("VNP"))
                ? p.PaidAt : p.PaidAt!.Value.AddHours(7),
            Received = p.Amount, Refunded = 0m
        });
        var refunds = payments.Where(p => p.Status == PaymentStatus.Refunded).Select(p => new
        {
            LocalAt = (p.ManualRefundedAt ?? (p.Method == PaymentMethod.AtCounter
                && p.ResponseMessage == CounterRefundMessage ? p.UpdatedAt : null))!.Value.AddHours(7),
            Received = 0m, Refunded = p.Amount
        });
        return receipts.Concat(refunds.Select(p => new { LocalAt = (DateTime?)p.LocalAt, p.Received, p.Refunded }))
            .Where(p => p.LocalAt >= lower && p.LocalAt < upper)
            .GroupBy(p => p.LocalAt!.Value.Date)
            .OrderByDescending(g => g.Key)
            .Select(g => new RevenueDay(g.Key, g.Sum(p => p.Received), g.Sum(p => p.Refunded)))
            .ToListAsync(cancellationToken);
    }
}
