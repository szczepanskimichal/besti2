namespace besti2.Application.Bookings;

public static class SlotCalculator
{
    public static readonly TimeOnly OpensAt = new(9, 0);
    public static readonly TimeOnly ClosesAt = new(17, 0);
    public const int SlotMinutes = 30;

    private static readonly TimeZoneInfo Oslo = TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");

    public static IReadOnlyList<DateTime> GetFreeSlots(
        DateOnly date,
        int durationMinutes,
        IReadOnlyList<(DateTime StartUtc, DateTime EndUtc)> busy,
        DateTime nowUtc)
    {
        var slots = new List<DateTime>();

        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return slots;
        }

        var duration = TimeSpan.FromMinutes(durationMinutes);
        var closesUtc = ToUtc(date, ClosesAt);

        for (var startUtc = ToUtc(date, OpensAt); startUtc + duration <= closesUtc; startUtc = startUtc.AddMinutes(SlotMinutes))
        {
            if (startUtc > nowUtc && IsFree(startUtc, startUtc + duration, busy))
            {
                slots.Add(startUtc);
            }
        }

        return slots;
    }

    private static bool IsFree(DateTime startUtc, DateTime endUtc, IReadOnlyList<(DateTime StartUtc, DateTime EndUtc)> busy)
    {
        foreach (var b in busy)
        {
            if (startUtc < b.EndUtc && b.StartUtc < endUtc)
            {
                return false;
            }
        }

        return true;
    }

    private static DateTime ToUtc(DateOnly date, TimeOnly time)
    {
        var osloTime = date.ToDateTime(time);
        return TimeZoneInfo.ConvertTimeToUtc(osloTime, Oslo);
    }
}