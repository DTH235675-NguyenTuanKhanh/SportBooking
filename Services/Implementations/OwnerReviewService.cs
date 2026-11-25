using Microsoft.EntityFrameworkCore;
using SportBooking.Data;
using SportBooking.Helpers;
using SportBooking.Models.Entities;
using SportBooking.Models.ViewModels.Owner;
using SportBooking.Services.Interfaces;

namespace SportBooking.Services.Implementations;

public sealed class OwnerReviewService(ApplicationDbContext context) : IOwnerReviewService
{
    private const int PageSize = 12;

    public async Task<OwnerReviewList> ListAsync(string ownerId, int page)
    {
        var query = OwnedBy(ownerId);
        var totalPages = (int)Math.Ceiling(await query.CountAsync() / (double)PageSize);
        page = Math.Clamp(page, 1, Math.Max(1, totalPages));
        var reviews = await SelectItem(query.OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id).Skip((page - 1) * PageSize).Take(PageSize)).ToListAsync();
        return new() { Reviews = reviews, Page = page, TotalPages = totalPages };
    }

    public Task<OwnerReviewItem?> FindAsync(string ownerId, int id) =>
        SelectItem(OwnedBy(ownerId).Where(r => r.Id == id)).SingleOrDefaultAsync();

    public async Task<ServiceResult> ReplyAsync(string ownerId, int id, string? reply)
    {
        var text = reply?.Trim();
        if (string.IsNullOrEmpty(text) || text.Length > 1000)
            return ServiceResult.Fail("Nhập phản hồi từ 1 đến 1.000 ký tự.");

        var visible = OwnedBy(ownerId).Where(r => r.Id == id && r.IsVisible);
        var recordedAt = ScheduleGuard.UtcNow;
        var changed = await visible.ExecuteUpdateAsync(update => update
            .SetProperty(r => r.OwnerReply, text)
            .SetProperty(r => r.OwnerReplyAt, recordedAt));
        return changed == 1
            ? ServiceResult.Ok("Đã lưu phản hồi.")
            : ServiceResult.Fail("Đánh giá không còn hiển thị hoặc không thuộc sân của bạn.");
    }

    private IQueryable<Review> OwnedBy(string ownerId) => context.Reviews.IgnoreQueryFilters().AsNoTracking()
        .Where(r => r.Court.SportComplex.OwnerId == ownerId);

    private static IQueryable<OwnerReviewItem> SelectItem(IQueryable<Review> query) =>
        query.Select(r => new OwnerReviewItem
        {
            Id = r.Id, Court = r.Court.Name, Complex = r.Court.SportComplex.Name, Customer = r.Customer.FullName,
            BookingCode = r.Booking.BookingCode, Rating = r.Rating, Comment = r.Comment, Reply = r.OwnerReply,
            ReplyAt = r.OwnerReplyAt, Visible = r.IsVisible, CourtDeleted = r.Court.IsDeleted
        });
}
