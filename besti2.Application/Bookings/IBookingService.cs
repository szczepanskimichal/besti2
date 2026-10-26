using besti2.Domain.Enums;

namespace besti2.Application.Bookings;

public interface IBookingService
{
    // units = how many times the service length in a row (more than 1 only for "Per time" services)
    Task<IReadOnlyList<DateTime>?> GetFreeSlotsAsync(
        Guid serviceId, Guid employeeId, DateOnly date, int units = 1, CancellationToken cancellationToken = default);
    // "Velg dag": free times for several days in a row; employeeId null = anyone free (Hvem som helst)
    Task<IReadOnlyList<FreeDayDto>?> GetFreeDaysAsync(
        Guid serviceId, Guid? employeeId, DateOnly from, int days, int units = 1,
        CancellationToken cancellationToken = default);
    // customerUserId = the logged-in customer's account id, saved on the booking
    Task<CreateBookingResult> CreateAsync(
        CreateBookingRequest request, string? customerUserId = null, CancellationToken cancellationToken = default);
    // Business panel: the business adds a visit itself (confirmed at once)
    Task<CreateBookingResult> CreateByBusinessAsync(
        Guid businessId, CreateManualBookingRequest request, CancellationToken cancellationToken = default);
        // Business panel: move a visit to another time and/or employee (same service and length)
        Task<MoveBookingResult> MoveAsync(
        Guid bookingId, Guid businessId, MoveBookingRequest request, CancellationToken cancellationToken = default);
        // "Ny tid": the moved visit behind a one-time link (null = wrong or used link)
        Task<NewTimeDto?> GetNewTimeAsync(string token, CancellationToken cancellationToken = default);
        // The customer accepts or declines the new time, from the e-mail link or from Min Besti
        Task<NewTimeOutcome> AnswerNewTimeAsync(string token, bool accept, CancellationToken cancellationToken = default);
        Task<NewTimeOutcome> AnswerNewTimeByCustomerAsync(Guid bookingId, string customerUserId, bool accept,
            CancellationToken cancellationToken = default);
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
    /// Customer cancels their own booking before it starts; message = optional reason for the business
    Task<UpdateBookingStatusOutcome> CancelByCustomerAsync(
        Guid bookingId, string customerUserId, string? message = null, CancellationToken cancellationToken = default);
}