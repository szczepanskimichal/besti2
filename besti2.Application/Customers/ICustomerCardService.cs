namespace besti2.Application.Customers;

public interface ICustomerCardService
{
    // Newest visit first; q = part of the name, phone or e-mail
    Task<IReadOnlyList<CustomerRowDto>> ListAsync(Guid businessId, string? q, DateTime nowUtc,
        CancellationToken cancellationToken = default);

    // null = no such customer at this business
    Task<CustomerCardDto?> GetAsync(Guid businessId, string key, DateTime nowUtc,
        CancellationToken cancellationToken = default);

    // Empty text removes the note; null = saved, otherwise the error
    Task<string?> SetNoteAsync(Guid businessId, string key, string? note,
        CancellationToken cancellationToken = default);

    // The notes for these customer keys, for the calendar's booking details
    Task<IReadOnlyDictionary<string, string>> NotesAsync(Guid businessId, IEnumerable<string> keys,
        CancellationToken cancellationToken = default);
}
