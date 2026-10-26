namespace besti2.Application.Bookings;

// One day in the "Velg dag" strip: all free start times that day
public record FreeDayDto(DateOnly Date, IReadOnlyList<FreeTimeDto> Slots);

// EmployeeId = who is free at that time (needed later for the booking)
public record FreeTimeDto(DateTime StartUtc, Guid EmployeeId);