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

    // The newest reviews on all businesses (to follow up a bad experience early)
    Task<IReadOnlyList<AdminReviewDto>> GetLatestReviewsAsync(int count, CancellationToken cancellationToken = default);
}
