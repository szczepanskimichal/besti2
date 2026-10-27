using System.Globalization;
using besti2.Application.Bookings;

namespace besti2.Application.Common;

public static class OsloTime
{
    private static readonly CultureInfo Norwegian = new("nb-NO");

    // "torsdag 22. oktober kl. 10:00"
    public static string Format(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(utc, SlotCalculator.Oslo).ToString("dddd d. MMMM 'kl.' HH:mm", Norwegian);
}