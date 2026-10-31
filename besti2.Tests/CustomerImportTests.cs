using besti2.Application.Customers;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace besti2.Tests;

// "Importer kunder": a customer list from another system (Timma, Fresha, Excel...) goes into Kunder
public class CustomerImportTests
{
    // Monday 7 January 2030, 10:00 in Oslo
    private static readonly DateTime Now = new(2030, 1, 7, 9, 0, 0, DateTimeKind.Utc);
    private static readonly Guid KlippOgStil = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid FjordBygg = new("22222222-2222-2222-2222-222222222222");

    private static async Task<AppDbContext> CreateSeededDbAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static Booking NewBooking(DateTime startUtc, string? userId, string phone, string name) => new()
    {
        Id = Guid.NewGuid(),
        StartUtc = startUtc,
        EndUtc = startUtc.AddMinutes(30),
        CreatedUtc = startUtc.AddDays(-3),
        CustomerName = name,
        CustomerPhone = phone,
        CustomerEmail = userId is null ? "" : "kari@test.example",
        CustomerUserId = userId,
        Status = BookingStatus.Bekreftet,
        PriceNok = 450,
        ServiceId = new Guid("a1111111-0000-0000-0000-000000000002"),
        EmployeeId = new Guid("b1111111-0000-0000-0000-000000000001")
    };

    private static CustomerImportRequest Request(bool save, params CustomerImportRow[] rows) =>
        new(rows, save, Confirmed: save);

    // check if the preview saves nothing, and saving adds new customers once, skips the ones already here and bad rows
    [Fact]
    public async Task Import_PreviewThenSave_AddsOnlyNewCustomers()
    {
        await using var db = await CreateSeededDbAsync();
        // Kari has an account, Per was added by phone
        db.Bookings.AddRange(
            NewBooking(Now.AddDays(-10), "kunde-1", "+4791234567", "Kari Nordmann"),
            NewBooking(Now.AddDays(-5), null, "922 22 222", "Per Hansen"));
        await db.SaveChangesAsync();
        var import = new CustomerImportService(db);
        CustomerImportRow[] rows =
        [
            new(2, "Kari  Nordmann", "0047 912 34 567", "kari@test.example", "Liker det kort"),
            new(3, "Per Hansen", "+47 922-22-222", null, null),
            new(4, "Ola Normann", "4793333333", "OLA@test.example", "Vil ha te"),
            new(5, "Ola N.", "933 33 333", null, null),
            new(6, "", "944 44 444", null, null),
            new(7, "Lise", "123", null, null),
            new(8, "Nina Berg", "(+47) 955 55 555", "nina-ugyldig", null)
        ];

        var (preview, error) = await import.ImportAsync(KlippOgStil, Request(false, rows));
        Assert.Null(error);
        Assert.False(preview!.Saved);
        Assert.Equal(2, preview.Added);
        Assert.Empty(db.ImportedCustomers);

        var (result, _) = await import.ImportAsync(KlippOgStil, Request(true, rows));
        Assert.True(result!.Saved);
        Assert.Equal(2, result.Added);
        Assert.Equal(2, result.Existing);
        Assert.Equal(1, result.Duplicates);
        Assert.Equal(2, result.Skipped);
        Assert.Equal(2, result.NotesAdded);
        Assert.Equal([6, 7, 8], result.Problems.Select(p => p.Line));

        var ola = await db.ImportedCustomers.SingleAsync(c => c.PhoneKey == "93333333");
        Assert.Equal("Ola Normann", ola.Name);
        Assert.Equal("ola@test.example", ola.Email);
        Assert.Null((await db.ImportedCustomers.SingleAsync(c => c.PhoneKey == "95555555")).Email);
        // Kari's note from the old system goes to her account card
        Assert.Equal("Liker det kort", (await db.CustomerNotes.SingleAsync(n => n.CustomerUserId == "kunde-1")).Text);

        // The same file again adds nobody, and the business's own notes stay
        var (again, _) = await import.ImportAsync(KlippOgStil, Request(true, rows));
        Assert.Equal(0, again!.Added);
        Assert.Equal(0, again.NotesAdded);
        Assert.Equal(2, await db.ImportedCustomers.CountAsync());
    }

    // check if saving needs the confirmation and a too big file is refused
    [Fact]
    public async Task Import_NeedsConfirmationAndAFileThatIsNotTooBig()
    {
        await using var db = await CreateSeededDbAsync();
        var import = new CustomerImportService(db);
        var row = new CustomerImportRow(2, "Ola", "93333333", null, null);

        Assert.NotNull((await import.ImportAsync(KlippOgStil, new CustomerImportRequest([row], true, false))).Error);
        Assert.NotNull((await import.ImportAsync(KlippOgStil, new CustomerImportRequest([], true, true))).Error);
        var tooMany = Enumerable.Range(0, CustomerImportService.MaxRows + 1)
            .Select(i => row with { Line = i + 2 }).ToList();
        Assert.NotNull((await import.ImportAsync(KlippOgStil, new CustomerImportRequest(tooMany, true, true))).Error);
        Assert.Empty(db.ImportedCustomers);
    }

    // check if imported customers show in Kunder with a card and a note, only at their own business, and can be removed
    [Fact]
    public async Task ImportedCustomers_ShowInKunderAndCanBeRemoved()
    {
        await using var db = await CreateSeededDbAsync();
        var import = new CustomerImportService(db);
        await import.ImportAsync(KlippOgStil, Request(true,
            new CustomerImportRow(2, "Ola Normann", "933 33 333", "ola@test.example", null),
            new CustomerImportRow(3, "Per Hansen", "922 22 222", "per@test.example", null)));
        var cards = new CustomerCardService(db);

        var rows = await cards.ListAsync(KlippOgStil, null, Now);
        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.True(r.Imported));
        Assert.Single(await cards.ListAsync(KlippOgStil, "933", Now));
        Assert.Empty(await cards.ListAsync(FjordBygg, null, Now));

        Assert.Null(await cards.SetNoteAsync(KlippOgStil, "t-93333333", "Vil ha te"));
        var card = await cards.GetAsync(KlippOgStil, "t-93333333", Now);
        Assert.Equal("Ola Normann", card!.Customer.Name);
        Assert.Equal("Vil ha te", card.Note);
        Assert.Empty(card.Visits);
        Assert.Null(await cards.GetAsync(FjordBygg, "t-93333333", Now));

        // Per books by phone without an e-mail: one customer, with the imported e-mail
        db.Bookings.Add(NewBooking(Now.AddDays(-1), null, "+47 922 22 222", "Per Hansen"));
        await db.SaveChangesAsync();
        rows = await cards.ListAsync(KlippOgStil, null, Now);
        var per = rows.Single(r => r.Key == "t-92222222");
        Assert.False(per.Imported);
        Assert.Equal(1, per.Visits);
        Assert.Equal("per@test.example", per.Email);
        Assert.Equal(2, rows.Count);

        Assert.False(await import.RemoveAsync(FjordBygg, "t-93333333"));
        Assert.False(await import.RemoveAsync(KlippOgStil, "k-kunde-1"));
        Assert.True(await import.RemoveAsync(KlippOgStil, "t-93333333"));
        Assert.Null(await cards.GetAsync(KlippOgStil, "t-93333333", Now));
        Assert.Empty(db.CustomerNotes);
    }

    // check if phone numbers from exports are read in the usual ways and nonsense is refused
    [Theory]
    [InlineData("+47 912 34 567", "+47 912 34 567")]
    [InlineData("912-34-567", "91234567")]
    [InlineData("(+47) 91234567", "+47 91234567")]
    [InlineData("004791234567", "+4791234567")]
    [InlineData("4791234567", "4791234567")]
    [InlineData("+46 70 123 45 67", "+46 70 123 45 67")]
    [InlineData("9,12E+07", null)]
    [InlineData("1234", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void NormalizePhone_ReadsExportFormats(string? input, string? expected) =>
        Assert.Equal(expected, CustomerImportService.NormalizePhone(input));

    // check if imported customers older than 3 years are deleted by the night job
    [Fact]
    public async Task Anonymizer_DeletesImportedCustomersAfterThreeYears()
    {
        await using var db = await CreateSeededDbAsync();
        db.ImportedCustomers.AddRange(
            new ImportedCustomer
            {
                Id = Guid.NewGuid(), BusinessId = KlippOgStil, Name = "Gammel", Phone = "93333333",
                PhoneKey = "93333333", ImportedUtc = Now.AddYears(-3).AddDays(-1)
            },
            new ImportedCustomer
            {
                Id = Guid.NewGuid(), BusinessId = KlippOgStil, Name = "Ny", Phone = "94444444",
                PhoneKey = "94444444", ImportedUtc = Now.AddDays(-1)
            });
        await db.SaveChangesAsync();

        await new BookingAnonymizer(db).AnonymizeOldBookingsAsync(Now);

        Assert.Equal("Ny", (await db.ImportedCustomers.SingleAsync()).Name);
    }
}
