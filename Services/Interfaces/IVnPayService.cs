namespace SportBooking.Services.Interfaces;

public interface IVnPayService
{
    Task<VnPayNotificationResult> ProcessNotificationAsync(IQueryCollection query, CancellationToken cancellationToken);
    Task<ServiceResult<int>> GetReturnBookingAsync(string customerId, IQueryCollection query);
}

public sealed record VnPayNotificationResult(string RspCode, string Message);
