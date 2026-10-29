namespace besti2.Application.Businesses;

// BreakStart/BreakEnd: a daily break (e.g. lunch) nobody can book; both null = no break
public record OpeningHoursDto(
    DayOfWeek DayOfWeek,
    TimeOnly OpensAt,
    TimeOnly ClosesAt,
    TimeOnly? BreakStart = null,
    TimeOnly? BreakEnd = null
);