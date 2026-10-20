namespace besti2.Application.Reviews;

public record CreateReviewRequest(Guid BookingId, int Stars, string? Comment);
public record ReplyToReviewRequest(string Reply);
public record ReviewDto(Guid Id, int Stars, string? Comment, string CustomerName, DateTime CreatedUtc,
    
    string? BusinessReply);
// Average is null when there are no reviews yet
public record BusinessReviewsDto(double? Average, int Count, IReadOnlyList<ReviewDto> Reviews);
