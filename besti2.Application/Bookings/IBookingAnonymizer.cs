namespace besti2.Application.Bookings;

public interface IBookingAnonymizer
{
// Removes customer data from bookings that ended more than 5 years before nowUtc and the names
// on their reviews; returns how many bookings and reviews were changed
    Task<int> AnonymizeOldBookingsAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
}