using Microsoft.EntityFrameworkCore;
using SportBooking.Constants;
using SportBooking.Data;
#if SPORTBOOKING_E2E
using System.Globalization;
#endif

namespace SportBooking.Helpers;

public static class ScheduleGuard
{
    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(UtcNow,
        TimeZoneInfo.FindSystemTimeZoneById(AppConstants.VietnamTimeZoneId));

    public static DateTime UtcNow
    {
        get
        {
#if SPORTBOOKING_E2E
            if (TestUtcNow.HasValue)
                return TestUtcNow.Value;
#endif
            return DateTime.UtcNow;
        }
    }

#if SPORTBOOKING_E2E
    private static DateTime? TestUtcNow { get; set; }

    internal static void ConfigureTestClock(IHostEnvironment environment)
    {
        if (!environment.IsEnvironment("Testing"))
            throw new InvalidOperationException("Bản build SB09 chỉ được chạy trong môi trường Testing.");

        var value = Environment.GetEnvironmentVariable("SPORTBOOKING_TEST_UTC_NOW");
        if (!DateTime.TryParseExact(value, "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var instant))
            throw new InvalidOperationException("SPORTBOOKING_TEST_UTC_NOW phải là thời điểm UTC dạng yyyy-MM-ddTHH:mm:ssZ.");

        TestUtcNow = instant;
    }
#endif

    // Every schedule writer and booking creator takes this lock inside its transaction.
    public static async Task LockAsync(ApplicationDbContext context, int complexId)
    {
        var resource = $"SportBooking:Complex:{complexId}";
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive',
                @LockOwner='Transaction', @LockTimeout=15000;
            IF @result < 0 THROW 51000, 'Could not acquire schedule lock.', 1;
            """);
    }
}
