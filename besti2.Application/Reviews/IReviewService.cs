namespace besti2.Application.Reviews;

public interface IReviewService
{
    // Returns null when saved, otherwise an error message for the customer
    Task<string?> CreateAsync(string customerUserId, CreateReviewRequest request,
        CancellationToken cancellationToken = default);

    Task<BusinessReviewsDto> GetForBusinessAsync(Guid businessId, CancellationToken cancellationToken = default);

    // Returns null when saved, otherwise an error message for the business
    Task<string?> ReplyAsync(Guid businessId, Guid reviewId, string reply,
        CancellationToken cancellationToken = default);
}