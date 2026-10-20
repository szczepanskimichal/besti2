using besti2.Domain.Enums;

namespace besti2.Application.Bookings;

public interface IBookingService
{
    Task<IReadOnlyList<DateTime>?> GetFreeSlotsAsync(
        Guid serviceId, Guid employeeId, DateOnly date, CancellationToken cancellationToken = default);
    // customerUserId = the logged-in customer's account id, saved on the booking
    Task<CreateBookingResult> CreateAsync(
        CreateBookingRequest request, string? customerUserId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PanelBookingDto>> GetForBusinessAsync(
        Guid businessId, CancellationToken cancellationToken = default);
    Task<UpdateBookingStatusOutcome> UpdateStatusAsync(
        Guid bookingId, Guid businessId, BookingStatus newStatus, CancellationToken cancellationToken = default);
    // Customer: own bookings, newest first
    Task<IReadOnlyList<CustomerBookingDto>> GetForCustomerAsync(
        string customerUserId, CancellationToken cancellationToken = default);
    // Customer cancels their own booking before it starts
    Task<UpdateBookingStatusOutcome> CancelByCustomerAsync(
        Guid bookingId, string customerUserId, CancellationToken cancellationToken = default);
}