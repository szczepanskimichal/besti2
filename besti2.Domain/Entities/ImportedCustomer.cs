namespace besti2.Domain.Entities;

// "Kunder" → "Importer kunder": a customer the business brought from its old system (Timma, Fresha, Excel...).
// Shown in Kunder next to the customers made from bookings; nobody gets an e-mail or SMS about it.
// PhoneKey = the digits of CustomerKey ("t-{PhoneKey}"), so a later booking with the same phone is the same customer
public class ImportedCustomer
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Business Business { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string PhoneKey { get; set; } = null!;
    public string? Email { get; set; }
    public DateTime ImportedUtc { get; set; }
}
