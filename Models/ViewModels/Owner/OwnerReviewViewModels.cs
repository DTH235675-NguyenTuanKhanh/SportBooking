namespace SportBooking.Models.ViewModels.Owner;

public sealed class OwnerReviewList
{
    public IReadOnlyList<OwnerReviewItem> Reviews { get; init; } = [];
    public int Page { get; init; }
    public int TotalPages { get; init; }
}

public sealed class OwnerReviewItem
{
    public int Id { get; init; }
    public string Court { get; init; } = "";
    public string Complex { get; init; } = "";
    public string Customer { get; init; } = "";
    public string BookingCode { get; init; } = "";
    public int Rating { get; init; }
    public string? Comment { get; init; }
    public string? Reply { get; init; }
    public DateTime? ReplyAt { get; init; }
    public bool Visible { get; init; }
    public bool CourtDeleted { get; init; }
}
