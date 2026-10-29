namespace besti2.Application.Bookings;

public static class SlotCalculator
{
    public const int SlotMinutes = 30;

    // The one Oslo time zone for the whole app (opening hours, booking days, mails)
    public static readonly TimeZoneInfo Oslo = TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");

    public static IReadOnlyList<DateTime> GetFreeSlots(
        DateOnly date,
        TimeOnly opensAt,
        TimeOnly closesAt,
        int durationMinutes,
        IReadOnlyList<(DateTime StartUtc, DateTime EndUtc)> busy,
        DateTime nowUtc,
        TimeOnly? breakStart = null,
        TimeOnly? breakEnd = null)
    {
        var slots = new List<DateTime>();
        // The daily break (lunch) is busy like a booking
        if (breakStart is TimeOnly pauseStart && breakEnd is TimeOnly pauseEnd && pauseEnd > pauseStart)
        {
            busy = [.. busy, (ToUtc(date, pauseStart), ToUtc(date, pauseEnd))];
        }

        var duration = TimeSpan.FromMinutes(durationMinutes);
        var closesUtc = ToUtc(date, closesAt);

        for (var startUtc = ToUtc(date, opensAt);
             startUtc + duration <= closesUtc;
             startUtc = startUtc.AddMinutes(SlotMinutes))
        {
            if (startUtc > nowUtc && IsFree(startUtc, startUtc + duration, busy))
            {
                slots.Add(startUtc);
            }
        }

        return slots;
    }

    // BESTI sammen: a time is free for N people when N different employees are free at that time.
    // Every employee's own free times go in; out come the shared times with all employees free then
    // (in the order they came in, so the first one gets the booking, like "Hvem som helst")
    public static IReadOnlyList<(DateTime StartUtc, IReadOnlyList<Guid> EmployeeIds)> FreeForPersons(
        IEnumerable<(Guid EmployeeId, IReadOnlyList<DateTime> Slots)> freeByEmployee, int persons)
    {
        return freeByEmployee
            .SelectMany(e => e.Slots.Select(start => (Start: start, e.EmployeeId)))
            .GroupBy(x => x.Start)
            .Select(g => (StartUtc: g.Key, EmployeeIds: (IReadOnlyList<Guid>)g.Select(x => x.EmployeeId).ToList()))
            .Where(x => x.EmployeeIds.Count >= persons)
            .OrderBy(x => x.StartUtc)
            .ToList();
    }

    private static bool IsFree(DateTime startUtc, DateTime endUtc,
        IReadOnlyList<(DateTime StartUtc, DateTime EndUtc)> busy)
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

    // A day and time in Oslo -> UTC (also used by the business calendar for "from midnight")
    public static DateTime ToUtc(DateOnly date, TimeOnly time)
    {
        var osloTime = date.ToDateTime(time);
        // 02:00–03:00 does not exist on the night clocks go forward; use 03:00 instead of crashing
        if (Oslo.IsInvalidTime(osloTime))
        {
            osloTime = osloTime.AddHours(1);
        }

        return TimeZoneInfo.ConvertTimeToUtc(osloTime, Oslo);
    }
}