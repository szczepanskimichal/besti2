namespace besti2.Application.Staff;

// "Egen innlogging" for an employee: the owner invites an e-mail, the employee connects their own BESTI account
// and then sees only their own calendar under /ansatt (no prices, statistics or settings)
public record StaffInviteRequest(string? Email);

// Panel → Ansatte: who has access. Connected = the employee has accepted
public record StaffAccessDto(Guid EmployeeId, string? Email, bool Connected, DateTime? InviteExpiresUtc);

// /ansatt/invitasjon: what the link is for, before the employee accepts
public record StaffInviteInfoDto(string BusinessName, string EmployeeName, string Email);

// /ansatt: one place of work (an account can be employee in more than one business)
public record StaffPlaceDto(
    Guid EmployeeId,
    string BusinessName,
    string BusinessSlug,
    string EmployeeName,
    IReadOnlyList<StaffBookingDto> Bookings,
    IReadOnlyList<StaffBlockDto> Blocks);

public record StaffBookingDto(
    Guid Id,
    DateTime StartUtc,
    DateTime EndUtc,
    string Status,
    string ServiceName,
    string CustomerName,
    string CustomerPhone,
    string? CustomerNote,
    string? SalonNote);

public record StaffBlockDto(DateTime StartUtc, DateTime EndUtc, string? Reason);
