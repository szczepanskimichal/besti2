namespace besti2.Application.Businesses;

public record ServiceDto(
    Guid Id,
    string Name,
    string? Description,
    int DurationMinutes,
    decimal? PriceNok,
    string PriceType
    );