using System.Reflection.Emit;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
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
            e.Property(x => x.Description).HasMaxLength(1000);
            e.HasData(
                new Business
                {
                    Id = new Guid("11111111-1111-1111-1111-111111111111"),
                    Name = "Klipp & Stil Frisør",
                    OrgNumber = "999000001",
                    Category = BusinessCategory.Frisor,
                    Description = "Hårklipp og farging for hele familien.",
                    Email = "post@klippogstil.example",
                    PhoneNumber = "+4722000001",
                    Address = "Karl Johans gate 10",
                    PostalCode = "0154",
                    City = "Oslo"
                },
                new Business
                {
                    Id = new Guid("22222222-2222-2222-2222-222222222222"),
                    Name = "Fjord Bygg AS",
                    OrgNumber = "999000002",
                    Category = BusinessCategory.Bygg,
                    Description = "Befaring og oppussing av bad og kjøkken.",
                    Email = "kontakt@fjordbygg.example",
                    PhoneNumber = "+4755000002",
                    Address = "Bryggen 5",
                    PostalCode = "5003",
                    City = "Bergen"
                },
                new Business
                {
                    Id = new Guid("33333333-3333-3333-3333-333333333333"),
                    Name = "Nordlys Psykologsenter",
                    OrgNumber = "999000003",
                    Category = BusinessCategory.Helse,
                    Description = "Samtaleterapi for voksne.",
                    Email = "time@nordlyspsykolog.example",
                    PhoneNumber = "+4773000003",
                    Address = "Munkegata 20",
                    PostalCode = "7011",
                    City = "Trondheim"
                });
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