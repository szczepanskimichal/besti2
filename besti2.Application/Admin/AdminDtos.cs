namespace besti2.Application.Admin;

// The admin page for the BESTI team: approvals, businesses, reviews. Only numbers and the business's
// own contact details, no customer lists
public record AdminOverviewDto(
    int VisibleBusinesses,
    int WaitingForApproval,
    int UnconfirmedSignups,
    int CustomerAccounts,
    int BookingsLast7Days,
    int VisitsLast30Days,
    int ReviewsLast30Days,
    int ProInterested,
    IReadOnlyList<AdminBusinessRow> Waiting,
    IReadOnlyList<AdminBusinessRow> NeedHelp,
    IReadOnlyList<AdminWeekDto> Weeks,
    IReadOnlyList<AdminBusinessRow> ProLeads);

// "Utvikling": one row per week (Monday in Oslo), the newest last
public record AdminWeekDto(DateOnly WeekStart, int NewBusinesses, int NewCustomers, int Bookings);

// Status: "Venter" (waits for approval), "Synlig", "Ubekreftet" (the owner never confirmed the e-mail),
// "Stengt" (closed by BESTI), "Skjult".
// SetupDone of SetupTotal: the "Kom i gang" steps the business has done
public record AdminBusinessRow(
    Guid Id,
    string Name,
    string Slug,
    string City,
    string Category,
    string Status,
    DateTime CreatedUtc,
    string? OwnerEmail,
    string PhoneNumber,
    bool Verified,
    int SetupDone,
    int SetupTotal,
    int BookingsLast30Days,
    int Upcoming,
    double? AverageStars,
    bool ProInterest,
    bool HasNote);

public record AdminSetupStep(string Title, bool Done);

public record AdminBusinessDetailsDto(
    AdminBusinessRow Business,
    string OrgNumber,
    string Address,
    string PostalCode,
    string? PublicEmail,
    string? Website,
    string? ReferralSource,
    DateTime? ApprovedUtc,
    string? AdminNote,
    bool WeeklyReportOn,
    int ReviewCount,
    DateTime? LastBookingCreatedUtc,
    IReadOnlyList<AdminSetupStep> Setup,
    DateTime? SuspendedUtc = null,
    string? SuspendedReason = null,
    IReadOnlyList<AdminActionDto>? Log = null);

public record AdminReviewDto(
    Guid Id,
    string BusinessName,
    string BusinessSlug,
    Guid BusinessId,
    string CustomerName,
    int Stars,
    string? Comment,
    string? BusinessReply,
    DateTime CreatedUtc,
    string? PhotoUrl = null,
    DateTime? HiddenUtc = null,
    string? HiddenReason = null);

public record AdminNoteRequest(string? Note);

// Hide a review / close a business: one of the fixed reasons (see AdminService), shown to the people it concerns
public record AdminReasonRequest(string? Reason);

// "Kunder" in /admin, for support ("jeg fikk aldri e-posten"). Only found by the exact e-mail or phone number:
// there is no list of all customers
public record AdminCustomerDto(
    string Id,
    string Email,
    string? FullName,
    string? Phone,
    bool EmailConfirmed,
    bool HasPassword,
    bool HasGoogle,
    DateTime? CreatedUtc,
    int Visits,
    int Reviews,
    IReadOnlyList<AdminCustomerBookingDto> Upcoming,
    IReadOnlyList<AdminMailDto> WaitingMails,
    IReadOnlyList<string> StaffAt);

public record AdminCustomerBookingDto(Guid Id, string BusinessName, string ServiceName, DateTime StartUtc,
    string Status);

// An e-mail still in the outbox (not sent yet, or failing)
public record AdminMailDto(string To, string Subject, DateTime CreatedUtc, int Attempts, DateTime NextAttemptUtc);

// "Systemstatus"
public record AdminHealthDto(
    string? Version,
    DateTime StartedUtc,
    bool DatabaseOk,
    double? DatabaseSizeMb,
    DateTime? LastBackupUtc,
    int MailsWaiting,
    int MailsFailing,
    IReadOnlyList<AdminMailDto> FailingMails,
    int PushDevices,
    IReadOnlyList<AdminJobDto> Jobs);

public record AdminJobDto(string Name, DateTime LastRunUtc, bool Ok);

// "Logg": what the BESTI team did
public record AdminActionDto(DateTime CreatedUtc, string AdminEmail, string Text, Guid? BusinessId);
