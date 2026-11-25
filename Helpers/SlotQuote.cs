using SportBooking.Models.Entities;

namespace SportBooking.Helpers;

// A client quote detects stale selections; price and availability still come from the database.
public static class SlotQuote
{
    public static string Create(TimeSlot slot, DateTime date)
    {
        var day = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? DayType.Weekend : DayType.Weekday;
        return FormattableString.Invariant($"{slot.CourtId}/{slot.Id}/{date:yyyyMMdd}/{slot.StartTime.Ticks}/{slot.EndTime.Ticks}/{slot.GetPriceByDayType(day):0.00}");
    }
}
