namespace besti2.Application.Bookings;

// "År" in the panel calendar: how many visits (not cancelled) start on one day in Oslo
public record BookingDayCountDto(DateOnly Date, int Count);
