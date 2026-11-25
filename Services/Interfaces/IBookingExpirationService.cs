namespace SportBooking.Services.Interfaces;

/// <summary>
/// Dọn các booking đang Pending đã quá hạn.
/// </summary>
public interface IBookingExpirationService
{
    Task<int> ExpirePendingBookingsAsync(CancellationToken cancellationToken = default);
}
