using besti2.Domain.Enums;

namespace besti2.Application.Bookings;

public interface IBookingService
{
    Task<IReadOnlyList<DateTime>?> GetFreeSlotsAsync(
        Guid serviceId, Guid employeeId, DateOnly date, CancellationToken cancellationToken = default);
    // customerUserId = the logged-in customer's account id, saved on the booking
    Task<CreateBookingResult> CreateAsync(
        CreateBookingRequest request, string? customerUserId = null, CancellationToken cancellationToken = default);
// Business panel: the business adds a visit itself (confirmed at once)
    Task<CreateBookingResult> CreateByBusinessAsync(
        Guid businessId, CreateManualBookingRequest request, CancellationToken cancellationToken = default);
        // Business panel: move a visit to another time and/or employee (same service and length)
        Task<MoveBookingResult> MoveAsync(
        Guid bookingId, Guid businessId, MoveBookingRequest request, CancellationToken cancellationToken = default);
        // Customer: own booking to another free time (the same rules as a new online booking)
        Task<MoveBookingResult> ChangeTimeByCustomerAsync(
            Guid bookingId, string customerUserId, ChangeTimeRequest request, CancellationToken cancellationToken = default);
        // from/to = days in Oslo time for the calendar; without them: upcoming bookings and the last 14 days
        Task<IReadOnlyList<PanelBookingDto>> GetForBusinessAsync(
        Guid businessId, DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default);
        // message = optional text for the customer in the "avlyst" e-mail
        Task<UpdateBookingStatusOutcome> UpdateStatusAsync(
            Guid bookingId, Guid businessId, BookingStatus newStatus, string? message = null,
            CancellationToken cancellationToken = default);
    // Customer: own bookings, newest first
    Task<IReadOnlyList<CustomerBookingDto>> GetForCustomerAsync(
        string customerUserId, CancellationToken cancellationToken = default);
    // Customer cancels their own booking before it starts
    Task<UpdateBookingStatusOutcome> CancelByCustomerAsync(
        Guid bookingId, string customerUserId, CancellationToken cancellationToken = default);
}