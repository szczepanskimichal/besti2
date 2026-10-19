namespace besti2.Application.Bookings;

public interface IBookingAnonymizer
{
// Removes customer data from bookings that ended more than 5 years before nowUtc; returns how many
    Task<int> AnonymizeOldBookingsAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
}