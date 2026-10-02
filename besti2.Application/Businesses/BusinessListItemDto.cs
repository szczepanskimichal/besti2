namespace besti2.Application.Businesses;

public record BusinessListItemDto(
    Guid Id,
    string Name,
    string Category,
    string? Description,
    string City
);
