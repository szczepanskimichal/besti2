namespace besti2.Application.Businesses;

public record CreateServiceRequest(
    string Name,
    string? Description,
    int DurationMinutes,
    decimal? PriceNok,
    string PriceType
);