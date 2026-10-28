namespace besti2.Application.Bookings;

public interface IAvailabilityService
{
    // units = how many times the service length in a row (more than 1 only for "Per time" services)
    Task<IReadOnlyList<DateTime>?> GetFreeSlotsAsync(
        Guid serviceId, Guid employeeId, DateOnly date, int units = 1, CancellationToken cancellationToken = default);

    // "Velg dag": free times for several days in a row; employeeId null = anyone free (Hvem som helst).
    // persons > 1 = BESTI sammen: only times when that many different employees are free (employeeId one of them)
    Task<IReadOnlyList<FreeDayDto>?> GetFreeDaysAsync(
        Guid serviceId, Guid? employeeId, DateOnly from, int days, int units = 1, int persons = 1,
        CancellationToken cancellationToken = default);

    // BESTI sammen: who can take the visits at exactly this time, the chosen employee first
    // (null = unknown service or employee, empty = not free for that many people)
    Task<IReadOnlyList<Guid>?> GetFreeEmployeesAsync(
        Guid serviceId, Guid? employeeId, DateTime startUtc, int units, int persons,
        CancellationToken cancellationToken = default);
}