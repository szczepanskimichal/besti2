namespace besti2.Infrastructure.Services.Email;

// Counts e-mails that could not be sent since the last check, so BookingReminderJob can tell healthchecks.io
// (an expired Brevo key would otherwise only show up in the logs). One API process, so a static counter is enough.
public static class MailFailures
{
    private static int count;

    public static void Record() => Interlocked.Increment(ref count);

    // How many failed since the last call, and starts counting again from 0
    public static int Take() => Interlocked.Exchange(ref count, 0);
}
