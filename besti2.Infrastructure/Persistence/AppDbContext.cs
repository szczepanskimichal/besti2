using System.Reflection.Emit;
using besti2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options){}

    public DbSet<Salon> Salons { get; set; } = null!;
    public DbSet<Employee> Employees { get; set; } = null!;
    public DbSet<Service> Services { get; set; } = null!;
    public DbSet<Booking> Bookings { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Service>(e =>
        {
            e.Property((x => x.Name)).IsRequired().HasMaxLength(200);
            e.Property((x => x.PriceNok)).HasPrecision(10, 2);
        });
        
        modelBuilder.Entity<Booking>(e =>
        {
            e.HasIndex(x => new { x.EmployeeId, x.StartUtc });
            e.HasOne(x => x.Employee).WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x=> x.Service).WithMany()
                .HasForeignKey(x => x.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

}