namespace besti2.Domain.Entities;

// An e-mail waiting to be sent (see EmailOutbox). Kept in the database, so a deploy or a crash of the API
// loses no e-mails. The row is deleted once the e-mail is sent or given up.
public class OutboxEmail
{
    public Guid Id { get; set; }
    public string To { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
    public DateTime CreatedUtc { get; set; }

    // How many times sending already failed
    public int Attempts { get; set; }

    // Not sent before this time: the wait before a retry, or the time a sender has to finish this e-mail
    public DateTime NextAttemptUtc { get; set; }
}
