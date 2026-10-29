namespace besti2.Domain.Entities;

// "Logg" in /admin: what the BESTI team did and when (approve, hide a review, close a business ...)
public class AdminAction
{
    public Guid Id { get; set; }

    // The admin's e-mail (Admin:Emails)
    public string AdminEmail { get; set; } = null!;

    // Short and in Norwegian, e.g. "Skjulte vurdering av Klipp & Stil: Personopplysninger"
    public string Text { get; set; } = null!;

    // The business it was about (null for e.g. a customer account); kept even if the business is deleted
    public Guid? BusinessId { get; set; }
    public DateTime CreatedUtc { get; set; }
}
