using besti2.Domain.Entities;
using besti2.Domain.Enums;

namespace besti2.Application.Bookings;

// Rules for online bookings by customers (a business adding or moving a visit itself is not limited)
public static class BookingRules
{
    // The salon must have time to see the booking: the earliest start is one hour from now
    public const int MinNoticeMinutes = 60;

    // One account cannot fill a salon's calendar: at most this many coming visits at one business
    public const int MaxUpcomingPerBusiness = 3;

    // No visit is longer than 8 hours (a service and "Per time" both stop at 480 minutes), so a visit that
    // touches a time window started at most this long before it. Searches use it as a lower bound,
    // otherwise the database reads every old booking of the employee
    public const int MaxVisitMinutes = 480;

    // The first time a customer can book
    public static DateTime EarliestStartUtc(DateTime nowUtc) => nowUtc.AddMinutes(MinNoticeMinutes);

    // The price saved on a new booking: the fixed price, or the hourly price times the hours booked.
    // Per m², per meter and by agreement have no price before the job is done (null)
    public static decimal? PriceFor(Service service, int units) => service.PriceType switch
    {
        PriceType.FastPris => service.PriceNok,
        PriceType.PerTime => service.PriceNok * Math.Max(units, 1),
        _ => null
    };
}
