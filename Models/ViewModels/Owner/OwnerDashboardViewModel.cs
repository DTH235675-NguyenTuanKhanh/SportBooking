namespace SportBooking.Models.ViewModels.Owner;

public class OwnerDashboardViewModel
{
    public string OwnerName { get; set; } = string.Empty;
    public DateTime Today { get; set; }
    public int ComplexCount { get; set; }
    public int CourtCount { get; set; }
    public int PendingCount { get; set; }
    public OwnerBookingListViewModel TodayBookings { get; set; } = new();
}
