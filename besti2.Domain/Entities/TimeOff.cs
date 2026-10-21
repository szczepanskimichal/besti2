// Blocked time in the calendar (holiday, doctor, closed day); nobody can book it

namespace besti2.Domain.Entities;
public class TimeOff
{
    public Guid Id { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    // Short note for the business itself ("Ferie", "Lege"); customers never see it
    public string? Reason { get; set; }

    public Guid BusinessId { get; set; }
    public Business Business { get; set; } = null!;
    // null = the whole business is closed, otherwise only this employee
    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
}