namespace besti2.Application.Businesses;

public record BusinessListItemDto(
    Guid Id,
    string Name,
    string Category,
    string? Description,
    string City,
    string? LogoUrl,
    IReadOnlyList<FreeSlotDto>? FreeSlots, // null when searching without a date
    double? DistanceKm = null, // only with "Nær meg"
    double? AverageRating = null, // e.g. 4.7; null when nobody has reviewed yet
    int ReviewCount = 0
);