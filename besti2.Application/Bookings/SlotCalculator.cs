namespace besti2.Application.Bookings;

public static class SlotCalculator
{
    public const int SlotMinutes = 30;

    private static readonly TimeZoneInfo Oslo = TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");

    public static IReadOnlyList<DateTime> GetFreeSlots(
        DateOnly date,
        TimeOnly opensAt,
        TimeOnly closesAt,
        int durationMinutes,
        IReadOnlyList<(DateTime StartUtc, DateTime EndUtc)> busy,
        DateTime nowUtc)
    {
        var slots = new List<DateTime>();
        
        var duration = TimeSpan.FromMinutes(durationMinutes);
        var closesUtc = ToUtc(date, closesAt);

        for (var startUtc = ToUtc(date, opensAt); startUtc + duration <= closesUtc; startUtc = startUtc.AddMinutes(SlotMinutes))
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