namespace besti2.Application.Bookings;

// "Flytt": a new day/time (Oslo) and maybe another employee; NotifyCustomer = send the new time by e-mail
public record MoveBookingRequest(Guid EmployeeId, DateOnly Date, TimeOnly Time, bool NotifyCustomer);

public enum MoveBookingOutcome
{
    Moved,
    NotFound,
    Invalid,
    SlotTaken
}

// Error = the message for the form when Invalid
public record MoveBookingResult(MoveBookingOutcome Outcome, string? Error = null);