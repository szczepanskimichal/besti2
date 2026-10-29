using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace besti2.Infrastructure.Identity;

public static class DevUserSeeder
{
    // Development only: one owner account per seeded business
    private const string DevPassword = "Test123!";

    private static readonly (string Email, Guid BusinessId)[] Owners =
    [
        ("eier@klippogstil.example", new Guid("11111111-1111-1111-1111-111111111111")),
        ("eier@fjordbygg.example", new Guid("22222222-2222-2222-2222-222222222222")),
        ("eier@nordlyspsykolog.example", new Guid("33333333-3333-3333-3333-333333333333")),
    ];

    // A customer for Min Besti and Favoritter, and an admin for /admin (Admin:Emails in appsettings.Development.json)
    private const string CustomerEmail = "kunde@besti.example";
    private const string AdminEmail = "admin@besti.example";

    private static readonly Guid KlippOgStil = new("11111111-1111-1111-1111-111111111111");

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        foreach (var (email, businessId) in Owners)
        {
            await CreateAsync(userManager, new AppUser
                { UserName = email, Email = email, EmailConfirmed = true, BusinessId = businessId });
        }

        await CreateAsync(userManager, new AppUser
            { UserName = CustomerEmail, Email = CustomerEmail, EmailConfirmed = true, FullName = "Kari Kunde" });
        await CreateAsync(userManager, new AppUser
            { UserName = AdminEmail, Email = AdminEmail, EmailConfirmed = true, FullName = "Ada Admin" });

        var customer = await userManager.FindByEmailAsync(CustomerEmail);
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedPastVisitsAsync(dbContext, customer!.Id);
        await SeedStampCardAsync(dbContext);
    }

    // A stamp card for Klipp & Stil that started 90 days ago, so Kari Kunde already has stamps.
    // Only when it was never set up (turning it off keeps the reward text, so it is not turned on again)
    private static async Task SeedStampCardAsync(AppDbContext dbContext)
    {
        var business = await dbContext.Businesses.FindAsync(KlippOgStil);
        if (business is null || business.LoyaltyReward is not null)
            return;

        business.LoyaltyVisits = 10;
        business.LoyaltyReward = "Gratis herreklipp";
        business.LoyaltyStartUtc = DateTime.UtcNow.AddDays(-90);
        await dbContext.SaveChangesAsync();
    }

    private static async Task CreateAsync(UserManager<AppUser> userManager, AppUser user)
    {
        if (await userManager.FindByEmailAsync(user.Email!) is not null)
            return;

        var result = await userManager.CreateAsync(user, DevPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Could not create {user.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
    }

    // About 12 weeks of visits for Klipp & Stil, so Statistikk has something to show (only once, the same every time)
    private static async Task SeedPastVisitsAsync(AppDbContext dbContext, string customerId)
    {
        var nowUtc = DateTime.UtcNow;
        if (await dbContext.Bookings.AnyAsync(b => b.Service.BusinessId == KlippOgStil && b.StartUtc < nowUtc))
            return;

        var services = await dbContext.Services.Where(s => s.BusinessId == KlippOgStil && !s.IsDeleted).ToListAsync();
        var employees = await dbContext.Employees.Where(e => e.BusinessId == KlippOgStil && !e.IsDeleted)
            .Select(e => e.Id).ToListAsync();
        if (services.Count == 0 || employees.Count == 0)
            return;

        var random = new Random(42);
        string[] names = ["Kari", "Ola", "Ingrid", "Per", "Nora", "Jonas", "Emma", "Lars", "Sara", "Ali", "Maja", "Even"];
        for (var daysAgo = 84; daysAgo >= 1; daysAgo--)
        {
            var day = nowUtc.Date.AddDays(-daysAgo);
            if (day.DayOfWeek is DayOfWeek.Sunday)
                continue;

            // Quieter on Mondays, busier at the end of the week
            var visits = random.Next(0, day.DayOfWeek switch { DayOfWeek.Monday => 2, DayOfWeek.Friday => 6, _ => 4 });
            for (var i = 0; i < visits; i++)
            {
                var service = services[random.Next(services.Count)];
                var name = names[random.Next(names.Length)];
                var mine = name == "Kari";
                var startUtc = day.AddHours(8 + i * 2 + (i % 2 == 0 ? 0 : 0.5));
                var roll = random.Next(100);
                dbContext.Bookings.Add(new Booking
                {
                    Id = Guid.NewGuid(),
                    StartUtc = startUtc,
                    EndUtc = startUtc.AddMinutes(service.DurationMinutes),
                    CreatedUtc = startUtc.AddDays(-random.Next(1, 14)),
                    CustomerName = mine ? "Kari Kunde" : name + " Demo",
                    CustomerPhone = $"+479000{random.Next(1000, 9999)}",
                    CustomerEmail = mine ? CustomerEmail : $"{name.ToLowerInvariant()}@demo.example",
                    // Every third visit is added by the salon itself (phone or walk-in)
                    CustomerUserId = mine ? customerId : random.Next(3) == 0 ? null : $"demo-{name}",
                    Status = roll < 6 ? BookingStatus.MotteIkke : roll < 14 ? BookingStatus.Avlyst : BookingStatus.Bekreftet,
                    PriceNok = service.PriceType == PriceType.FastPris ? service.PriceNok : null,
                    ServiceId = service.Id,
                    EmployeeId = employees[i % employees.Count],
                    ReminderSentUtc = startUtc.AddDays(-1)
                });
            }
        }

        await dbContext.SaveChangesAsync();
    }
}