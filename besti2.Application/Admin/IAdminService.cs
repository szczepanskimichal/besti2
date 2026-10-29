namespace besti2.Application.Admin;

public interface IAdminService
{
    Task<AdminOverviewDto> GetOverviewAsync(DateTime nowUtc, CancellationToken cancellationToken = default);

    // status = "Venter", "Synlig", "Ubekreftet" or null for all; q = part of the name, city, org number or e-mail
    Task<IReadOnlyList<AdminBusinessRow>> GetBusinessesAsync(string? status, string? q, DateTime nowUtc,
        CancellationToken cancellationToken = default);

    Task<AdminBusinessDetailsDto?> GetBusinessAsync(Guid id, DateTime nowUtc,
        CancellationToken cancellationToken = default);

    // BESTI's own note (who called, what they need help with); null = saved, otherwise the error
    Task<string?> SetNoteAsync(Guid id, string? note, CancellationToken cancellationToken = default);

    // The newest reviews on all businesses (to follow up a bad experience early); onlyHidden = the hidden ones
    Task<IReadOnlyList<AdminReviewDto>> GetLatestReviewsAsync(int count, bool onlyHidden = false,
        CancellationToken cancellationToken = default);

    // Hide a review (one of ReviewReasons) or show it again; the customer and the business are told.
    // null = done, otherwise the error
    Task<string?> HideReviewAsync(Guid reviewId, string? reason, string adminEmail,
        CancellationToken cancellationToken = default);

    Task<string?> ShowReviewAsync(Guid reviewId, string adminEmail, CancellationToken cancellationToken = default);

    // Close a visible business (one of SuspendReasons) or open it again; the owner is told
    Task<string?> SuspendBusinessAsync(Guid id, string? reason, string adminEmail,
        CancellationToken cancellationToken = default);

    Task<string?> ReopenBusinessAsync(Guid id, string adminEmail, CancellationToken cancellationToken = default);

    // The exact e-mail, or the phone number; never a list of everyone
    Task<IReadOnlyList<AdminCustomerDto>> FindCustomersAsync(string query, DateTime nowUtc,
        CancellationToken cancellationToken = default);

    Task<AdminHealthDto> GetHealthAsync(string? version, DateTime startedUtc, DateTime nowUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminActionDto>> GetLogAsync(int count, CancellationToken cancellationToken = default);

    // Writes one line to the log
    Task LogAsync(string adminEmail, string text, Guid? businessId, CancellationToken cancellationToken = default);
}
