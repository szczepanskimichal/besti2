namespace besti2.Application.Reviews;

public interface IReviewService
{
    // Returns null when saved, otherwise an error message for the customer
    Task<string?> CreateAsync(string customerUserId, CreateReviewRequest request,
        CancellationToken cancellationToken = default);

    // A photo from the visit on the customer's own review (one; a new one replaces it).
    // Returns null when saved, otherwise an error message
    Task<string?> SetPhotoAsync(string customerUserId, Guid bookingId, Stream content, long length,
        CancellationToken cancellationToken = default);

    // includeHidden = the business panel (shows reviews BESTI has hidden, with the reason);
    // viewerUserId = the logged-in reader, for MarkedHelpful; take = only the newest ones (the average and the
    // count are always from all of them)
    Task<BusinessReviewsDto> GetForBusinessAsync(Guid businessId, bool includeHidden = false,
        string? viewerUserId = null, int? take = null, CancellationToken cancellationToken = default);

    // Returns null when saved, otherwise an error message for the business
    Task<string?> ReplyAsync(Guid businessId, Guid reviewId, string reply,
        CancellationToken cancellationToken = default);

    // "Nyttig" on and off; null = no such review (or hidden, or the reader's own)
    Task<HelpfulResult?> ToggleHelpfulAsync(string userId, Guid reviewId, CancellationToken cancellationToken = default);
}
