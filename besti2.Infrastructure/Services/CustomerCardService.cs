using besti2.Application.Customers;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services;

// "Kunder": the business's customers made from its own bookings (no extra customer register),
// plus the business's own note about each one
public class CustomerCardService(AppDbContext dbContext) : ICustomerCardService
{
    public const int NoteMaxLength = 1000;

    // The list shows the newest customers first; a search finds the others
    public const int MaxRows = 300;

    public async Task<IReadOnlyList<CustomerRowDto>> ListAsync(Guid businessId, string? q, DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var visits = await VisitsAsync(businessId, null, cancellationToken);
        var notes = await NoteKeysAsync(businessId, cancellationToken);
        var imported = await ImportedAsync(businessId, null, cancellationToken);
        var search = q?.Trim().ToLowerInvariant();
        var searchDigits = string.IsNullOrEmpty(search) ? "" : CustomerKey.Digits(search);

        var rows = Group(visits)
            .Select(g => WithImportedEmail(Row(g.Key, g.ToList(), nowUtc, notes.Contains(g.Key)), imported))
            .ToList();
        // An imported customer who has booked since is already in the list through the bookings
        var bookedPhones = visits.Select(v => CustomerKey.Digits(v.CustomerPhone)).ToHashSet();
        rows.AddRange(imported.Values
            .Where(c => !bookedPhones.Contains(c.PhoneKey))
            .Select(c => ImportedRow(c, notes.Contains($"t-{c.PhoneKey}"))));

        return rows
            .Where(r => string.IsNullOrEmpty(search)
                        || r.Name.ToLowerInvariant().Contains(search)
                        || (r.Email?.ToLowerInvariant().Contains(search) ?? false)
                        || (searchDigits.Length >= 3 && CustomerKey.Digits(r.Phone).Contains(searchDigits)))
            .OrderByDescending(r => r.NextVisitUtc ?? r.LastVisitUtc)
            .ThenBy(r => r.Name)
            .Take(MaxRows)
            .ToList();
    }

    public async Task<CustomerCardDto?> GetAsync(Guid businessId, string key, DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        if (CustomerKey.Parse(key) is null)
        {
            return null;
        }

        var visits = Group(await VisitsAsync(businessId, key, cancellationToken))
            .FirstOrDefault(g => g.Key == key)?.ToList();
        var imported = await ImportedAsync(businessId, key, cancellationToken);
        if (visits is null)
        {
            // Imported from the old system and not booked here yet: contact and note only
            if (imported.Count == 0)
            {
                return null;
            }

            var importedNote = await FindNoteAsync(businessId, key, cancellationToken);
            return new CustomerCardDto(ImportedRow(imported.Values.Single(), importedNote is not null),
                importedNote?.Text, importedNote?.UpdatedUtc, 0, []);
        }

        var note = await FindNoteAsync(businessId, key, cancellationToken);
        var row = WithImportedEmail(Row(key, visits, nowUtc, note is not null), imported);
        return new CustomerCardDto(
            row,
            note?.Text,
            note?.UpdatedUtc,
            SpentNok: visits.Where(v => v.Status == BookingStatus.Bekreftet && v.EndUtc <= nowUtc)
                .Sum(v => v.PriceNok ?? 0),
            visits.OrderByDescending(v => v.StartUtc)
                .Take(100)
                .Select(v => new CustomerVisitDto(v.Id, v.StartUtc, v.ServiceName, v.EmployeeName,
                    v.Status.ToString(), v.PriceNok, v.CustomerNote))
                .ToList());
    }

    public async Task<string?> SetNoteAsync(Guid businessId, string key, string? note,
        CancellationToken cancellationToken = default)
    {
        if (CustomerKey.Parse(key) is not var (userId, phone))
        {
            return "Fant ikke kunden.";
        }

        var text = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (text?.Length > NoteMaxLength)
        {
            return $"Notatet kan være maks {NoteMaxLength} tegn.";
        }

        // Only a customer who has booked here or was imported
        if (!Group(await VisitsAsync(businessId, key, cancellationToken)).Any(g => g.Key == key)
            && (await ImportedAsync(businessId, key, cancellationToken)).Count == 0)
        {
            return "Fant ikke kunden.";
        }

        var existing = await FindNoteAsync(businessId, key, cancellationToken);
        if (text is null)
        {
            if (existing is not null)
            {
                dbContext.CustomerNotes.Remove(existing);
            }
        }
        else if (existing is null)
        {
            dbContext.CustomerNotes.Add(new CustomerNote
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                CustomerUserId = userId,
                CustomerPhone = phone,
                Text = text,
                UpdatedUtc = DateTime.UtcNow
            });
        }
        else
        {
            existing.Text = text;
            existing.UpdatedUtc = DateTime.UtcNow;
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            // Saved in two tabs at the same moment: the first one counts
            return "Notatet ble endret samtidig et annet sted. Last siden på nytt.";
        }

        return null;
    }

    public async Task<IReadOnlyDictionary<string, string>> NotesAsync(Guid businessId, IEnumerable<string> keys,
        CancellationToken cancellationToken = default)
    {
        var wanted = keys.ToHashSet();
        if (wanted.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        var notes = await dbContext.CustomerNotes.AsNoTracking()
            .Where(n => n.BusinessId == businessId)
            .Select(n => new { n.CustomerUserId, n.CustomerPhone, n.Text })
            .ToListAsync(cancellationToken);
        return notes
            .Select(n => (Key: n.CustomerUserId is not null ? $"k-{n.CustomerUserId}" : $"t-{n.CustomerPhone}", n.Text))
            .Where(n => wanted.Contains(n.Key))
            .ToDictionary(n => n.Key, n => n.Text);
    }

    private async Task<CustomerNote?> FindNoteAsync(Guid businessId, string key, CancellationToken cancellationToken)
    {
        var (userId, phone) = CustomerKey.Parse(key)!.Value;
        return userId is not null
            ? await dbContext.CustomerNotes.FirstOrDefaultAsync(
                n => n.BusinessId == businessId && n.CustomerUserId == userId, cancellationToken)
            : await dbContext.CustomerNotes.FirstOrDefaultAsync(
                n => n.BusinessId == businessId && n.CustomerPhone == phone, cancellationToken);
    }

    private async Task<HashSet<string>> NoteKeysAsync(Guid businessId, CancellationToken cancellationToken) =>
        (await dbContext.CustomerNotes.AsNoTracking()
            .Where(n => n.BusinessId == businessId)
            .Select(n => n.CustomerUserId != null ? "k-" + n.CustomerUserId : "t-" + n.CustomerPhone)
            .ToListAsync(cancellationToken))
        .ToHashSet();

    // Every visit of the business with customer data (anonymized ones have no customer any more).
    // key = only that customer's visits: one card must not read years of the whole salon's history
    private async Task<List<Visit>> VisitsAsync(Guid businessId, string? key, CancellationToken cancellationToken)
    {
        var bookings = dbContext.Bookings.AsNoTracking()
            .Where(b => b.Service.BusinessId == businessId && b.CustomerEmail != BookingAnonymizer.AnonymizedEmail);
        if (CustomerKey.Parse(key) is var (userId, phone))
        {
            // Phone numbers are saved with or without spaces and +47: the last digits pick them out,
            // Group then keeps only the exact same customer
            bookings = userId is not null
                ? bookings.Where(b => b.CustomerUserId == userId)
                : bookings.Where(b => b.CustomerUserId == null && b.CustomerPhone.Replace(" ", "").EndsWith(phone!));
        }

        return await bookings
            .Select(b => new Visit(b.Id, b.CustomerUserId, b.CustomerName, b.CustomerPhone, b.CustomerEmail,
                b.StartUtc, b.EndUtc, b.Status, b.Service.Name, b.Employee.Name + " " + b.Employee.LastName,
                b.PriceNok, b.CustomerNote))
            .ToListAsync(cancellationToken);
    }

    // The imported customers by phone digits; key = only that customer ("t-" keys only, never an account)
    private async Task<Dictionary<string, ImportedCustomer>> ImportedAsync(Guid businessId, string? key,
        CancellationToken cancellationToken)
    {
        var customers = dbContext.ImportedCustomers.AsNoTracking().Where(c => c.BusinessId == businessId);
        if (key is not null)
        {
            if (CustomerKey.Parse(key) is not (null, { } phone))
            {
                return [];
            }

            customers = customers.Where(c => c.PhoneKey == phone);
        }

        return await customers.ToDictionaryAsync(c => c.PhoneKey, cancellationToken);
    }

    private static CustomerRowDto ImportedRow(ImportedCustomer customer, bool hasNote) =>
        new($"t-{customer.PhoneKey}", customer.Name, customer.Phone, customer.Email, HasAccount: false,
            Visits: 0, NoShows: 0, LastVisitUtc: null, NextVisitUtc: null, hasNote, Imported: true);

    // A visit added by phone often has no e-mail: the imported one is shown instead
    private static CustomerRowDto WithImportedEmail(CustomerRowDto row, Dictionary<string, ImportedCustomer> imported) =>
        row.Email is null && row.Key.StartsWith("t-") && imported.TryGetValue(row.Key[2..], out var customer)
            ? row with { Email = customer.Email }
            : row;

    private static IEnumerable<IGrouping<string, Visit>> Group(List<Visit> visits) =>
        visits.Select(v => (Key: CustomerKey.For(v.CustomerUserId, v.CustomerPhone), Visit: v))
            .Where(x => x.Key is not null)
            .GroupBy(x => x.Key!, x => x.Visit);

    private static CustomerRowDto Row(string key, List<Visit> visits, DateTime nowUtc, bool hasNote)
    {
        // The newest booking has the name and phone the customer uses now
        var newest = visits.MaxBy(v => v.StartUtc)!;
        var done = visits.Where(v => v.Status == BookingStatus.Bekreftet && v.EndUtc <= nowUtc).ToList();
        return new CustomerRowDto(
            key,
            newest.CustomerName,
            newest.CustomerPhone,
            string.IsNullOrEmpty(newest.CustomerEmail) ? null : newest.CustomerEmail,
            HasAccount: newest.CustomerUserId is not null,
            Visits: done.Count,
            NoShows: visits.Count(v => v.Status == BookingStatus.MotteIkke),
            LastVisitUtc: done.Count == 0 ? null : done.Max(v => v.StartUtc),
            NextVisitUtc: visits.Where(v => v.StartUtc > nowUtc && v.Status != BookingStatus.Avlyst)
                .Select(v => (DateTime?)v.StartUtc).Min(),
            hasNote);
    }

    private sealed record Visit(
        Guid Id,
        string? CustomerUserId,
        string CustomerName,
        string CustomerPhone,
        string CustomerEmail,
        DateTime StartUtc,
        DateTime EndUtc,
        BookingStatus Status,
        string ServiceName,
        string EmployeeName,
        decimal? PriceNok,
        string? CustomerNote);
}
