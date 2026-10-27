namespace besti2.Application.Businesses;

public record UpdateOpeningHoursRequest(IReadOnlyList<OpeningHoursDto> Days);