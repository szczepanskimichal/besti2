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
}