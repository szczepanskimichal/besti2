namespace besti2.Application.Businesses;

public record OpeningHoursDto(
    DayOfWeek DayOfWeek,
    TimeOnly OpensAt,
    TimeOnly ClosesAt
    );