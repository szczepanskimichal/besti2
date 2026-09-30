namespace best2.Domain.Entities;

public class Salon
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null;
    public string Address { get; set; } = null;
    public string City { get; set; } = null;
    
    public List<Employee> Employees { get; set; } = new ();
    public List<Service> Services { get; set; } = new ();
}