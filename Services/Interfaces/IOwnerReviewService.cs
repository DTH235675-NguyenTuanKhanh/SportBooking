using SportBooking.Models.ViewModels.Owner;

namespace SportBooking.Services.Interfaces;

public interface IOwnerReviewService
{
    Task<OwnerReviewList> ListAsync(string ownerId, int page);
    Task<OwnerReviewItem?> FindAsync(string ownerId, int id);
    Task<ServiceResult> ReplyAsync(string ownerId, int id, string? reply);
}
