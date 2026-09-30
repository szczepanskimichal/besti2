namespace best2.Domain.Entities;

public class Service
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int DescriptionMinutes { get; set; }
    public decimal PriceNok { get; set; }
    
    public Guid SalonId { get; set; }
    public Salon Salon { get; set; } = null!;
}