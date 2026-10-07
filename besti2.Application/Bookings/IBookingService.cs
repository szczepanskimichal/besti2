using besti2.Domain.Enums;

namespace besti2.Application.Bookings;

public interface IBookingService
{
    Task<IReadOnlyList<DateTime>?> GetFreeSlotsAsync(
        Guid serviceId, Guid employeeId, DateOnly date, CancellationToken cancellationToken = default);
    Task<CreateBookingResult> CreateAsync(
        CreateBookingRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PanelBookingDto>> GetForBusinessAsync(
        Guid businessId, CancellationToken cancellationToken = default);
    Task<UpdateBookingStatusOutcome> UpdateStatusAsync(
        Guid bookingId, Guid businessId, BookingStatus newStatus, CancellationToken cancellationToken = default);
}