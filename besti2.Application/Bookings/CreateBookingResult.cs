namespace besti2.Application.Bookings;

public enum CreateBookingOutcome
{
    Created,
    Invalid,
    NotFound,
    SlotTaken,
    CustomerBusy
}

public record BookingCreatedDto(Guid Id, DateTime StartUtc, DateTime EndUtc, string Status);

public record CreateBookingResult(CreateBookingOutcome Outcome, BookingCreatedDto? Booking = null, string? Error = null);