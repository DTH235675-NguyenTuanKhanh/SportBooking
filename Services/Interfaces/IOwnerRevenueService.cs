using SportBooking.Models.ViewModels.Owner;

namespace SportBooking.Services.Interfaces;

public interface IOwnerRevenueService
{
    Task<OwnerRevenueViewModel> ReadAsync(string ownerId, OwnerRevenueFilter filter, bool validInput,
        CancellationToken cancellationToken);
}
