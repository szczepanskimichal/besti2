using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options){}

    public DbSet<Business> Businesses { get; set; } = null!;
    public DbSet<Employee> Employees { get; set; } = null!;
    public DbSet<Service> Services { get; set; } = null!;
    public DbSet<Booking> Bookings { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
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
                        e.HasData(
                new Service { Id = new Guid("a1111111-0000-0000-0000-000000000001"), Name = "Dameklipp", Description = "Klipp, vask og styling", DurationMinutes = 60, PriceType = PriceType.FastPris, PriceNok = 790m, BusinessId = new Guid("11111111-1111-1111-1111-111111111111") },
                new Service { Id = new Guid("a1111111-0000-0000-0000-000000000002"), Name = "Herreklipp", DurationMinutes = 30, PriceType = PriceType.FastPris, PriceNok = 450m, BusinessId = new Guid("11111111-1111-1111-1111-111111111111") },
                new Service { Id = new Guid("a1111111-0000-0000-0000-000000000003"), Name = "Farging", Description = "Inkludert vask og føn", DurationMinutes = 120, PriceType = PriceType.FastPris, PriceNok = 1490m, BusinessId = new Guid("11111111-1111-1111-1111-111111111111") },

                new Service { Id = new Guid("a2222222-0000-0000-0000-000000000001"), Name = "Befaring", Description = "Gratis befaring og tilbud", DurationMinutes = 60, PriceType = PriceType.EtterAvtale, PriceNok = null, BusinessId = new Guid("22222222-2222-2222-2222-222222222222") },
                new Service { Id = new Guid("a2222222-0000-0000-0000-000000000002"), Name = "Fliselegging", Description = "Bad og kjøkken", DurationMinutes = 60, PriceType = PriceType.PerKvadratmeter, PriceNok = 950m, BusinessId = new Guid("22222222-2222-2222-2222-222222222222") },
                new Service { Id = new Guid("a2222222-0000-0000-0000-000000000003"), Name = "Snekkerarbeid", DurationMinutes = 60, PriceType = PriceType.PerTime, PriceNok = 850m, BusinessId = new Guid("22222222-2222-2222-2222-222222222222") },

                new Service { Id = new Guid("a3333333-0000-0000-0000-000000000001"), Name = "Samtaleterapi", Description = "Individuell samtale, 50 minutter", DurationMinutes = 50, PriceType = PriceType.FastPris, PriceNok = 1200m, BusinessId = new Guid("33333333-3333-3333-3333-333333333333") },
                new Service { Id = new Guid("a3333333-0000-0000-0000-000000000002"), Name = "Første konsultasjon", DurationMinutes = 60, PriceType = PriceType.FastPris, PriceNok = 950m, BusinessId = new Guid("33333333-3333-3333-3333-333333333333") });
        });
        
        modelBuilder.Entity<Employee>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.LastName).IsRequired().HasMaxLength(100);
            e.HasData(
                new Employee { Id = new Guid("b1111111-0000-0000-0000-000000000001"), Name = "Ingrid", LastName = "Hansen", BusinessId = new Guid("11111111-1111-1111-1111-111111111111") },
                new Employee { Id = new Guid("b1111111-0000-0000-0000-000000000002"), Name = "Sofie", LastName = "Berg", BusinessId = new Guid("11111111-1111-1111-1111-111111111111") },
                new Employee { Id = new Guid("b2222222-0000-0000-0000-000000000001"), Name = "Lars", LastName = "Johansen", BusinessId = new Guid("22222222-2222-2222-2222-222222222222") },
                new Employee { Id = new Guid("b3333333-0000-0000-0000-000000000001"), Name = "Kari", LastName = "Nordmann", BusinessId = new Guid("33333333-3333-3333-3333-333333333333") });
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
            e.Property(x => x.CustomerName).IsRequired().HasMaxLength(100);
            e.Property(x => x.CustomerPhone).IsRequired().HasMaxLength(20);
            e.Property(x => x.StartUtc)
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            e.Property(x => x.EndUtc)
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });
    }

}