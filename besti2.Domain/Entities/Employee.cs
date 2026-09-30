namespace best2.Domain.Entities;

public class Employee
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null;
    public string LastName { get; set; } = null;
    
    public Guid SalonId { get; set; }
    public Salon Salon { get; set; } = null;
}