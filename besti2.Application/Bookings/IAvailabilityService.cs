namespace besti2.Application.Bookings;

public interface IAvailabilityService
{
    // units = how many times the service length in a row (more than 1 only for "Per time" services)
    Task<IReadOnlyList<DateTime>?> GetFreeSlotsAsync(
        Guid serviceId, Guid employeeId, DateOnly date, int units = 1, CancellationToken cancellationToken = default);

    // "Velg dag": free times for several days in a row; employeeId null = anyone free (Hvem som helst)
    Task<IReadOnlyList<FreeDayDto>?> GetFreeDaysAsync(
        Guid serviceId, Guid? employeeId, DateOnly from, int days, int units = 1,
        CancellationToken cancellationToken = default);
}