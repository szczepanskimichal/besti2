using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options){}
    // Every saved change to a notification rings that bell at once (see NotificationSignals and
    // /api/notifications/stream), wherever in the code the notification was written or read
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var bells = ChangeTracker.Entries<Notification>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => NotificationSignals.Key(e.Entity.UserId, e.Entity.BusinessId))
            .Distinct()
            .ToList();
        var saved = await base.SaveChangesAsync(cancellationToken);
        foreach (var bell in bells)
        {
            NotificationSignals.Changed(bell);
        }
        return saved;
    }
    public DbSet<Business> Businesses { get; set; } = null!;
    public DbSet<Employee> Employees { get; set; } = null!;
    public DbSet<Service> Services { get; set; } = null!;
    public DbSet<Booking> Bookings { get; set; } = null!;
    
    public DbSet<OpeningHours> OpeningHours { get; set; } = null!;
    public DbSet<BusinessImage> BusinessImages { get; set; } = null!;
    public DbSet<Review> Reviews { get; set; } = null!;
    public DbSet<TimeOff> TimeOffs { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;

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
                    City = "Oslo",
                    Latitude = 59.91181,
                    Longitude = 10.74646
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
                    City = "Bergen",
                    Latitude = 60.39612,
                    Longitude = 5.32553
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
                    City = "Trondheim",
                    Latitude = 63.42952,
                    Longitude = 10.39516
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
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.LastName).IsRequired().HasMaxLength(200);
            e.HasData(
                new Employee { Id = new Guid("b1111111-0000-0000-0000-000000000001"), Name = "Ingrid", LastName = "Hansen", BusinessId = new Guid("11111111-1111-1111-1111-111111111111") },
                new Employee { Id = new Guid("b1111111-0000-0000-0000-000000000002"), Name = "Sofie", LastName = "Berg", BusinessId = new Guid("11111111-1111-1111-1111-111111111111") },
                new Employee { Id = new Guid("b2222222-0000-0000-0000-000000000001"), Name = "Lars", LastName = "Johansen", BusinessId = new Guid("22222222-2222-2222-2222-222222222222") },
                new Employee { Id = new Guid("b3333333-0000-0000-0000-000000000001"), Name = "Kari", LastName = "Nordmann", BusinessId = new Guid("33333333-3333-3333-3333-333333333333") });
        });
        
        modelBuilder.Entity<Booking>(e =>
        {
            // One active booking per employee and start time; cancelled ones do not count
            e.HasIndex(x => new { x.EmployeeId, x.StartUtc })
                .IsUnique()
                .HasFilter("[Status] <> 'Avlyst'");
            e.HasIndex(x => x.CustomerUserId);
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
            e.Property(x => x.CustomerNote).HasMaxLength(500);
            e.Property(x => x.NewTimeTokenHash).HasMaxLength(64);
            e.HasIndex(x => x.NewTimeTokenHash);
            e.Property(x => x.PreviousStartUtc)
                .HasConversion(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);
            e.Property(x => x.StartUtc)
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            e.Property(x => x.EndUtc)
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            
        });
        modelBuilder.Entity<OpeningHours>(e =>
        {
            e.HasIndex(x => new { x.BusinessId, x.DayOfWeek }).IsUnique();
            e.HasData(
                Hours(1, KlippOgStilId, DayOfWeek.Monday, 9, 17),
                Hours(2, KlippOgStilId, DayOfWeek.Tuesday, 9, 17),
                Hours(3, KlippOgStilId, DayOfWeek.Wednesday, 9, 17),
                Hours(4, KlippOgStilId, DayOfWeek.Thursday, 9, 19),
                Hours(5, KlippOgStilId, DayOfWeek.Friday, 9, 17),
                Hours(6, KlippOgStilId, DayOfWeek.Saturday, 10, 15),
                Hours(7, FjordByggId, DayOfWeek.Monday, 7, 15),
                Hours(8, FjordByggId, DayOfWeek.Tuesday, 7, 15),
                Hours(9, FjordByggId, DayOfWeek.Wednesday, 7, 15),
                Hours(10, FjordByggId, DayOfWeek.Thursday, 7, 15),
                Hours(11, FjordByggId, DayOfWeek.Friday, 7, 15),
                Hours(12, NordlysId, DayOfWeek.Monday, 8, 16),
                Hours(13, NordlysId, DayOfWeek.Tuesday, 8, 16),
                Hours(14, NordlysId, DayOfWeek.Wednesday, 8, 16),
                Hours(15, NordlysId, DayOfWeek.Thursday, 8, 16));
        });
        modelBuilder.Entity<BusinessImage>(e =>
        {
            e.Property(x => x.Kind).HasConversion<string>().IsRequired().HasMaxLength(20);
            e.Property(x => x.FileName).IsRequired().HasMaxLength(100);
            e.Property(x => x.Caption).HasMaxLength(200);
            e.HasIndex(x => new { x.BusinessId, x.Kind });
        });
        modelBuilder.Entity<Review>(e =>
        {
            e.HasIndex(x => x.BookingId).IsUnique(); // one review per booking
            e.HasIndex(x => x.BusinessId);
            e.HasOne(x => x.Booking).WithMany()
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.CustomerUserId).IsRequired().HasMaxLength(450);
            e.Property(x => x.CustomerName).IsRequired().HasMaxLength(100);
            e.Property(x => x.Comment).HasMaxLength(500);
            e.Property(x => x.BusinessReply).HasMaxLength(500);
        });
        modelBuilder.Entity<Notification>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.CreatedUtc });
            e.HasIndex(x => new { x.BusinessId, x.CreatedUtc });
            e.Property(x => x.UserId).HasMaxLength(450);
            e.Property(x => x.Text).IsRequired().HasMaxLength(300);
            e.Property(x => x.Link).HasMaxLength(300);
            e.Property(x => x.CreatedUtc)
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });
        modelBuilder.Entity<TimeOff>(e =>
        {
            e.HasIndex(x => new { x.BusinessId, x.StartUtc });
            e.HasOne(x => x.Business).WithMany()
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
            // Restrict: SQL Server allows only one cascade path (Business -> Employee -> TimeOff would be a second)
            e.HasOne(x => x.Employee).WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Reason).HasMaxLength(100);
            e.Property(x => x.StartUtc)
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            e.Property(x => x.EndUtc)
                .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });
    }
        // Seed helpers for OpeningHours
            private static readonly Guid KlippOgStilId = new("11111111-1111-1111-1111-111111111111");
            private static readonly Guid FjordByggId = new("22222222-2222-2222-2222-222222222222");
            private static readonly Guid NordlysId = new("33333333-3333-3333-3333-333333333333");

            private static OpeningHours Hours(int number, Guid businessId, DayOfWeek day, int opensHour, int closesHour) => new()
            {
                Id = new Guid($"c0000000-0000-0000-0000-{number:D12}"),
                BusinessId = businessId,
                DayOfWeek = day,
                OpensAt = new TimeOnly(opensHour, 0),
                ClosesAt = new TimeOnly(closesHour, 0)
            };
}