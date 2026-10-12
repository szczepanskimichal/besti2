namespace besti2.Domain.Entities;

public class OpeningHours
{
    public Guid Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly OpensAt { get; set; }
    public TimeOnly ClosesAt { get; set; }

    public Guid BusinessId { get; set; }
    public Business Business { get; set; } = null!;
}