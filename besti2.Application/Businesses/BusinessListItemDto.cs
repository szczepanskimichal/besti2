namespace besti2.Application.Businesses;

public record BusinessListItemDto(
    Guid Id,
    string Name,
    string Category,
    string? Description,
    string City,
    string? LogoUrl,
    
    IReadOnlyList<FreeSlotDto>? FreeSlots, // null when searching without a date
    double? DistanceKm = null // only with "Nær meg"
);
