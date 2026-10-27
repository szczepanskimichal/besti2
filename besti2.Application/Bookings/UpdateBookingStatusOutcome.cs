namespace besti2.Application.Bookings;

public enum UpdateBookingStatusOutcome
{
    Updated,
    NotFound,
    InvalidTransition,

    // The business's free-cancel deadline has passed: the customer has to call them
    TooLate
}