namespace besti2.Domain.Entities;

public class Employee
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string LastName { get; set; } = null!;
    
    public Guid BusinessId { get; set; }
    public Business Business { get; set; } = null!;
}