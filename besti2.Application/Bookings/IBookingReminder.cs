namespace besti2.Application.Bookings;

public interface IBookingReminder
{
    // E-mails customers whose confirmed visit starts within 24 hours of nowUtc (once per visit); returns how many
    Task<int> SendRemindersAsync(DateTime nowUtc, CancellationToken cancellationToken = default);

    // E-mails each business whose new bookings still wait for "Bekreft" after a while (once per booking);
    // returns how many businesses got a mail
    Task<int> SendPendingRemindersAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
}