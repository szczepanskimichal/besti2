namespace besti2.Domain.Entities;

// "Tid for ny time?": the customer asked to be reminded to book this service again N weeks after each visit.
// Only when the customer asks for it (a service message, not marketing), in the bell and by push, never by mail
public class RebookReminder
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = null!;
    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    // The same employee next time (null = Hvem som helst)
    public Guid? EmployeeId { get; set; }

    public int Weeks { get; set; }
    public DateTime CreatedUtc { get; set; }

    // The visit the last reminder was about: one reminder per visit
    public Guid? LastRemindedBookingId { get; set; }
}
