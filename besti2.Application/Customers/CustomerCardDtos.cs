namespace besti2.Application.Customers;

// "Kunder" in the panel: everyone who has booked at the business, made from the bookings themselves.
// Key: "k-{account id}" for a customer account, "t-{phone}" for a visit added by phone (see CustomerKey)
public record CustomerRowDto(
    string Key,
    string Name,
    string Phone,
    string? Email,
    bool HasAccount,
    int Visits,
    int NoShows,
    DateTime? LastVisitUtc,
    DateTime? NextVisitUtc,
    bool HasNote);

public record CustomerVisitDto(
    Guid Id,
    DateTime StartUtc,
    string ServiceName,
    string EmployeeName,
    string Status,
    decimal? PriceNok,
    string? CustomerNote);

public record CustomerCardDto(
    CustomerRowDto Customer,
    string? Note,
    DateTime? NoteUpdatedUtc,
    decimal SpentNok,
    IReadOnlyList<CustomerVisitDto> Visits);

public record CustomerNoteRequest(string? Note);
