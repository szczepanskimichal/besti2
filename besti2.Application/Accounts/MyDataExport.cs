namespace besti2.Application.Accounts;

// "Last ned mine data" (GDPR art. 15 and 20): everything BESTI keeps about one customer, as one file
public record MyDataExport(
    DateTime ExportedUtc,
    ExportAccount Account,
    IReadOnlyList<ExportBooking> Bookings,
    IReadOnlyList<ExportReview> Reviews,
    IReadOnlyList<ExportNotification> Notifications);

public record ExportAccount(string FullName, string Email, string? Phone, bool EmailConfirmed);

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

public record ExportReview(DateTime CreatedUtc, string Business, int Stars, string? Comment, string? BusinessReply);

public record ExportNotification(DateTime CreatedUtc, string Text, bool IsRead);