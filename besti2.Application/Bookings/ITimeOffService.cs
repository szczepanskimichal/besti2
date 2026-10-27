namespace besti2.Application.Bookings;

public interface ITimeOffService
{
    // Blocked time that touches the days from-to (Oslo), also a holiday that started earlier
    Task<IReadOnlyList<TimeOffDto>> GetForBusinessAsync(Guid businessId, DateOnly from, DateOnly to,
        CancellationToken cancellationToken = default);

    // null = the employee does not belong to the business
    Task<CreateTimeOffResult?> CreateAsync(Guid businessId, CreateTimeOffRequest request,
        CancellationToken cancellationToken = default);

    // false = not found (or another business's)
    Task<bool> DeleteAsync(Guid businessId, Guid timeOffId, CancellationToken cancellationToken = default);
}