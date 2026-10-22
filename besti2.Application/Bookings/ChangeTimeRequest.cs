namespace besti2.Application.Bookings;

// "Endre tid" in Min Besti: one of the free times shown to the customer, maybe with another employee
public record ChangeTimeRequest(Guid EmployeeId, DateTime StartUtc);