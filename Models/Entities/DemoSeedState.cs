namespace SportBooking.Models.Entities;

/// <summary>Marks a fully committed demo dataset; ordinary business data is never a seed marker.</summary>
public sealed class DemoSeedState
{
    public string Key { get; set; } = string.Empty;
    public DateTime CompletedAtUtc { get; set; }
}
