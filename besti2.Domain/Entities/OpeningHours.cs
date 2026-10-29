namespace besti2.Domain.Entities;

public class OpeningHours
{
    public Guid Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly OpensAt { get; set; }
    public TimeOnly ClosesAt { get; set; }

    // A break every such day (e.g. lunch 12:00–12:30); nobody can book it. Both null = no break
    public TimeOnly? BreakStart { get; set; }
    public TimeOnly? BreakEnd { get; set; }

    public Guid BusinessId { get; set; }
    public Business Business { get; set; } = null!;
}