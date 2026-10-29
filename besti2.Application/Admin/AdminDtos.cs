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
    IReadOnlyList<AdminBusinessRow> NeedHelp);

// Status: "Venter" (waits for approval), "Synlig", "Ubekreftet" (the owner never confirmed the e-mail), "Skjult".
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
    IReadOnlyList<AdminSetupStep> Setup);

public record AdminReviewDto(
    Guid Id,
    string BusinessName,
    string BusinessSlug,
    Guid BusinessId,
    string CustomerName,
    int Stars,
    string? Comment,
    string? BusinessReply,
    DateTime CreatedUtc);

public record AdminNoteRequest(string? Note);
