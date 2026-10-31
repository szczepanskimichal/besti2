using besti2.Application.Bookings;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services.Bookings;
using Microsoft.EntityFrameworkCore;

namespace besti2.Tests;

// "Tid for ny time?": one reminder N weeks after each real visit, only for those who asked for it
public class RebookReminderServiceTests
{
    // Monday 7 January 2030, 12:00 in Oslo (inside 9-20)
    private static readonly DateTime Now = new(2030, 1, 7, 11, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Herreklipp = new("a1111111-0000-0000-0000-000000000002");
    private static readonly Guid Ingrid = new("b1111111-0000-0000-0000-000000000001");

    private static async Task<AppDbContext> CreateSeededDbAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static Booking Visit(DateTime startUtc, BookingStatus status = BookingStatus.Bekreftet) => new()
    {
        Id = Guid.NewGuid(),
        StartUtc = startUtc,
        EndUtc = startUtc.AddMinutes(30),
        CreatedUtc = startUtc.AddDays(-7),
        CustomerName = "Kari",
        CustomerPhone = "+4790000000",
        CustomerEmail = "kari@test.example",
        CustomerUserId = "kunde-1",
        Status = status,
        ServiceId = Herreklipp,
        EmployeeId = Ingrid
    };

    // check if a visit 6 weeks ago gives one reminder with a link to the same service and employee
    [Fact]
    public async Task SendDueAsync_SixWeeksAfterVisit_OneReminder()
    {
        await using var db = await CreateSeededDbAsync();
        db.Bookings.Add(Visit(Now.AddDays(-43)));
        await db.SaveChangesAsync();
        var service = new RebookReminderService(db);
        Assert.Null(await service.SetAsync("kunde-1", new SetRebookReminderRequest(Herreklipp, Ingrid, 6), Now));

        var first = await service.SendDueAsync(Now);
        var second = await service.SendDueAsync(Now.AddHours(1));

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        var notification = Assert.Single(await db.Notifications.ToListAsync());
        Assert.Equal("kunde-1", notification.UserId);
        Assert.Contains("6 uker", notification.Text);
        Assert.Equal($"/klipp-og-stil-frisor-oslo/bestill?tjeneste={Herreklipp}&ansatt={Ingrid}", notification.Link);
    }

    // check if there is no reminder too early, when already booked again, after "Møtte ikke", or at night
    [Fact]
    public async Task SendDueAsync_NoReminderWhenNotDue()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new RebookReminderService(db);
        await service.SetAsync("kunde-1", new SetRebookReminderRequest(Herreklipp, null, 6), Now);

        db.Bookings.Add(Visit(Now.AddDays(-20)));
        await db.SaveChangesAsync();
        Assert.Equal(0, await service.SendDueAsync(Now)); // only 3 weeks ago

        db.Bookings.Add(Visit(Now.AddDays(-80), BookingStatus.MotteIkke));
        await db.SaveChangesAsync();
        Assert.Equal(0, await service.SendDueAsync(Now)); // the last real visit is still the one 3 weeks ago

        var night = new DateTime(2030, 3, 1, 1, 0, 0, DateTimeKind.Utc); // 02:00 in Oslo
        Assert.Equal(0, await service.SendDueAsync(night));

        db.Bookings.Add(Visit(Now.AddDays(5))); // booked again
        await db.SaveChangesAsync();
        Assert.Equal(0, await service.SendDueAsync(Now.AddDays(30)));
        Assert.Empty(await db.Notifications.ToListAsync());
    }

    // "Velg selv": any number of weeks from 1 to 26 is fine, e.g. 5
    [Fact]
    public async Task SetAsync_ChosenWeeks_IsSaved()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new RebookReminderService(db);

        Assert.Null(await service.SetAsync("kunde-1", new SetRebookReminderRequest(Herreklipp, null, 5), Now));
        Assert.Null(await service.SetAsync("kunde-1", new SetRebookReminderRequest(Herreklipp, null, 26), Now));

        Assert.Equal(26, (await db.RebookReminders.SingleAsync()).Weeks);
    }

    // check if 0 weeks turns it off, a wrong number is refused, and one reminder per service
    [Fact]
    public async Task SetAsync_UpdatesTurnsOffAndChecks()
    {
        await using var db = await CreateSeededDbAsync();
        var service = new RebookReminderService(db);

        Assert.NotNull(await service.SetAsync("kunde-1", new SetRebookReminderRequest(Herreklipp, null, 27), Now));
        Assert.NotNull(await service.SetAsync("kunde-1", new SetRebookReminderRequest(Herreklipp, null, -1), Now));
        Assert.NotNull(await service.SetAsync("kunde-1", new SetRebookReminderRequest(Guid.NewGuid(), null, 6), Now));
        await service.SetAsync("kunde-1", new SetRebookReminderRequest(Herreklipp, null, 4), Now);
        await service.SetAsync("kunde-1", new SetRebookReminderRequest(Herreklipp, Ingrid, 8), Now);

        var mine = Assert.Single(await service.GetMineAsync("kunde-1"));
        Assert.Equal(8, mine.Weeks);
        Assert.Equal(Ingrid, mine.EmployeeId);
        Assert.False(await service.RemoveAsync("kunde-2", mine.Id));

        await service.SetAsync("kunde-1", new SetRebookReminderRequest(Herreklipp, null, 0), Now);
        Assert.Empty(await service.GetMineAsync("kunde-1"));
    }
}
