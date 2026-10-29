namespace besti2.Application.Reviews;

public record CreateReviewRequest(Guid BookingId, int Stars, string? Comment);

public record ReplyToReviewRequest(string Reply);

// PhotoUrl: the customer's photo from the visit (optional). Helpful = how many found it "Nyttig",
// MarkedHelpful = the logged-in reader did. Hidden/HiddenReason only reach the business panel
public record ReviewDto(
    Guid Id,
    int Stars,
    string? Comment,
    string CustomerName,
    DateTime CreatedUtc,
    string? BusinessReply,
    string? PhotoUrl = null,
    int Helpful = 0,
    bool MarkedHelpful = false,
    bool Hidden = false,
    string? HiddenReason = null);

// Average is null when there are no reviews yet (hidden reviews never count)
public record BusinessReviewsDto(double? Average, int Count, IReadOnlyList<ReviewDto> Reviews);

// "Nyttig" pressed: whether the reader now marks it, and the new count
public record HelpfulResult(bool Marked, int Helpful);
