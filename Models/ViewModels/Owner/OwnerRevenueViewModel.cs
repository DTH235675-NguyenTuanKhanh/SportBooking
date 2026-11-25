namespace SportBooking.Models.ViewModels.Owner;

public sealed class OwnerRevenueFilter
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public int? ComplexId { get; set; }
    public int? CourtId { get; set; }
}

public sealed record RevenueOption(int Id, string Name);
public sealed record RevenueDay(DateTime Day, decimal Received, decimal Refunded)
{
    public decimal Net => Received - Refunded;
}

public sealed class OwnerRevenueViewModel
{
    public OwnerRevenueFilter Filter { get; init; } = new();
    public IReadOnlyList<RevenueOption> Complexes { get; init; } = [];
    public IReadOnlyList<RevenueOption> Courts { get; init; } = [];
    public IReadOnlyList<RevenueDay> Days { get; set; } = [];
    public string? Error { get; set; }
    public int UndatedPayments { get; set; }
    public int BookingsWithoutPayment { get; set; }
    public decimal Received => Days.Sum(d => d.Received);
    public decimal Refunded => Days.Sum(d => d.Refunded);
    public decimal Net => Received - Refunded;
}
