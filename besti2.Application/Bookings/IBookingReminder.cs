namespace besti2.Application.Bookings;

public interface IBookingReminder
{
    // E-mails customers whose confirmed visit starts within 24 hours of nowUtc (once per visit); returns how many
    Task<int> SendRemindersAsync(DateTime nowUtc, CancellationToken cancellationToken = default);

    // "Om 2 timer": a notice in the bell (and so on the phone) about 2 hours before each confirmed visit of a
    // customer account (once per visit, not when turned off under Varsler); returns how many
    Task<int> SendSoonNoticesAsync(DateTime nowUtc, CancellationToken cancellationToken = default);

    // E-mails each business whose new bookings still wait for "Bekreft" after a while (once per booking);
    // returns how many businesses got a mail
    Task<int> SendPendingRemindersAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
}