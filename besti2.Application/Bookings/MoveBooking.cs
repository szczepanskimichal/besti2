namespace besti2.Application.Bookings;

// "Flytt": a new day/time (Oslo) and maybe another employee.
// AskCustomer = the customer must accept the new time (e-mail with a link); otherwise the move is final and
// NotifyCustomer = only tell the customer by e-mail (they already agreed, e.g. on the phone)
public record MoveBookingRequest(
    Guid EmployeeId,
    DateOnly Date,
    TimeOnly Time,
    bool NotifyCustomer,
    bool AskCustomer = false);

public enum MoveBookingOutcome
{
    Moved,
    NotFound,
    Invalid,
    SlotTaken
}

// Error = the message for the form when Invalid
public record MoveBookingResult(MoveBookingOutcome Outcome, string? Error = null);

// The customer's answer to a moved visit
public record NewTimeAnswerRequest(string Token, bool Accept);

public record NewTimeAnswer(bool Accept);

public enum NewTimeOutcome
{
    Accepted,
    Declined,
    NotFound, // wrong or used link
    TooLate // the visit has already started
}

// What the customer sees on the "Ny tid" page before answering
public record NewTimeDto(
    string BusinessName,
    string ServiceName,
    string EmployeeName,
    DateTime StartUtc,
    DateTime? PreviousStartUtc,
    string Status);