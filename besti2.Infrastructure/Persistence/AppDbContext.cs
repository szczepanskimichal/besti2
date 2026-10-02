using System.Reflection.Emit;
using besti2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options){}

    public DbSet<Business> Businesses { get; set; } = null!;
    public DbSet<Employee> Employees { get; set; } = null!;
    public DbSet<Service> Services { get; set; } = null!;
    public DbSet<Booking> Bookings { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Business>(e =>
        {
            e.Property((x => x.Name)).IsRequired().HasMaxLength(200);
            e.Property(x => x.OrgNumber).IsRequired().HasMaxLength(9);
            e.HasIndex(x => x.OrgNumber).IsUnique();
            e.Property(x => x.PostalCode).IsRequired().HasMaxLength(4);
            e.Property(x => x.Category).HasConversion<string>().IsRequired().HasMaxLength(100);
            e.Property(x => x.Email).IsRequired().HasMaxLength(250);
            e.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(20);
            e.Property(x => x.City).IsRequired().HasMaxLength(200);
        });
        modelBuilder.Entity<Service>(e =>
        {
            e.Property((x => x.Name)).IsRequired().HasMaxLength(200);
            e.Property((x => x.PriceNok)).HasPrecision(10, 2);
            e.Property(x => x.PriceType).HasConversion<string>().HasMaxLength(30);
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
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            e.Property(x=>x.CustomerEmail).IsRequired().HasMaxLength(250);

        });
    }

}