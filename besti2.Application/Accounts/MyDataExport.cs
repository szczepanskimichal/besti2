namespace besti2.Application.Accounts;

// "Last ned mine data" (GDPR art. 15 and 20): everything BESTI keeps about one customer, as one file
public record MyDataExport(
    DateTime ExportedUtc,
    ExportAccount Account,
    IReadOnlyList<ExportBooking> Bookings,
    IReadOnlyList<ExportReview> Reviews,
    IReadOnlyList<ExportNotification> Notifications,
    IReadOnlyList<ExportFavorite> Favorites,
    IReadOnlyList<ExportLoyaltyReward> LoyaltyRewards,
    IReadOnlyList<ExportWaitlistEntry> Waitlist,
    IReadOnlyList<ExportRebookReminder> RebookReminders,
    IReadOnlyList<ExportInvite> Invites,
    IReadOnlyList<ExportHelpfulVote> HelpfulVotes,
    IReadOnlyList<ExportPushDevice> PushDevices,
    IReadOnlyList<ExportStaffAccess> StaffAccess,
    IReadOnlyList<ExportInviteMessage>? InviteMessages = null);

// ReminderEmail / SoonNotice: the choices under Varsler
public record ExportAccount(string FullName, string Email, string? Phone, bool EmailConfirmed, bool ReminderEmail,
    bool SoonNotice);

public record ExportBooking(
    DateTime StartUtc,
    DateTime EndUtc,
    string Status,
    string Business,
    string Service,
    string Employee,
    string CustomerName,
    string CustomerPhone,
    string CustomerEmail,
    string? Note,
    bool MarketingConsent,
    DateTime? MarketingConsentUtc);

public record ExportReview(DateTime CreatedUtc, string Business, int Stars, string? Comment, string? BusinessReply,
    string? PhotoUrl, string? HiddenReason);

public record ExportNotification(DateTime CreatedUtc, string Text, bool IsRead);

public record ExportFavorite(DateTime CreatedUtc, string Business);

public record ExportLoyaltyReward(DateTime RedeemedUtc, string Business);

// "Si fra hvis det blir ledig"
public record ExportWaitlistEntry(DateOnly Date, string Business, string Service, DateTime CreatedUtc, bool Notified);

// "Tid for ny time?"
public record ExportRebookReminder(string Business, string Service, int Weeks, DateTime CreatedUtc);

// BESTI sammen: the invitations the customer sent
public record ExportInvite(DateTime CreatedUtc, string Business, string Service, DateTime StartUtc, int Persons,
    string Status, string Phone, string? Note);

// "Nyttig" on other customers' reviews
public record ExportHelpfulVote(DateTime CreatedUtc, string Business);

// "Varsler på telefonen": which push service each device uses (the address itself is a long secret)
public record ExportPushDevice(DateTime CreatedUtc, string PushService);

// "/ansatt": the businesses where the account sees its own work calendar
public record ExportStaffAccess(string Business, string Employee);

// BESTI-chat lines and greetings I wrote in invitations (Kind = Chat or Hilsen)
public record ExportInviteMessage(DateTime CreatedUtc, string Kind, string Business, string Service, string Text);
