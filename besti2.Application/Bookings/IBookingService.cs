namespace besti2.Application.Bookings;

public interface IBookingService
{
    Task<IReadOnlyList<DateTime>?> GetFreeSlotsAsync(
        Guid serviceId, Guid employeeId, DateOnly date, CancellationToken cancellationToken = default);
    Task<CreateBookingResult> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken = default);
}