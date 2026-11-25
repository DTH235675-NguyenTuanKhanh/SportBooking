using Microsoft.Extensions.Options;
using SportBooking.Models.ViewModels.Shared;
using SportBooking.Services.Interfaces;

namespace SportBooking.Services.Expiration;

/// <summary>
/// Định kỳ hết hạn booking đang chờ thanh toán hoặc xác nhận tại quầy.
/// </summary>
public class BookingExpirationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingExpirationBackgroundService> _logger;
    private readonly TimeSpan _scanInterval;

    public BookingExpirationBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<BookingSettings> settings,
        ILogger<BookingExpirationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _scanInterval = TimeSpan.FromSeconds(Math.Max(5, settings.Value.ExpirationScanIntervalSeconds));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ScanAsync(stoppingToken);

        using var timer = new PeriodicTimer(_scanInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ScanAsync(stoppingToken);
        }
    }

    private async Task ScanAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var expirationService = scope.ServiceProvider.GetRequiredService<IBookingExpirationService>();
            await expirationService.ExpirePendingBookingsAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Ứng dụng đang dừng.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi quét booking quá hạn. Tác vụ sẽ thử lại ở chu kỳ tiếp theo.");
        }
    }
}
