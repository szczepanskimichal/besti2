namespace besti2.Infrastructure.Services;

// Protects Brevo's 300 mails a day. Mails to addresses nobody has confirmed yet (sign-up links and
// password reset) get at most one mail per address every 5 minutes, and at most dailyLimit a day in
// total, so booking mails always get through. Kept in memory: an API restart starts from zero,
// which is fine with one server.
public class MailLimiter(int dailyLimit = 150, TimeSpan? cooldown = null)
{
    public const string TryLaterMessage =
        "Vi kan ikke sende en ny e-post til denne adressen akkurat nå. " +
        "Sjekk innboksen (også søppelpost) for lenken vi sendte, eller prøv igjen om noen minutter.";

    private readonly TimeSpan wait = cooldown ?? TimeSpan.FromMinutes(5);
    private readonly Dictionary<string, DateTime> lastMailUtc = new();
    private readonly Lock gate = new();
    private DateOnly today;
    private int sentToday;

    // true = send the mail (the slot is taken now); false = do not send
    public bool TryReserve(string email)
    {
        var now = DateTime.UtcNow;
        var key = email.Trim().ToLowerInvariant();
        lock (gate)
        {
            // A new day: Brevo starts counting again, so do we
            if (DateOnly.FromDateTime(now) != today)
            {
                today = DateOnly.FromDateTime(now);
                sentToday = 0;
                lastMailUtc.Clear();
            }

            if (sentToday >= dailyLimit)
            {
                return false;
            }

            if (lastMailUtc.TryGetValue(key, out var last) && now - last < wait)
            {
                return false;
            }

            lastMailUtc[key] = now;
            sentToday++;
            return true;
        }
    }
}