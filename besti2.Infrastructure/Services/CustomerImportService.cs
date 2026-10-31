using besti2.Application.Common;
using besti2.Application.Customers;
using besti2.Domain.Entities;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services;

// "Importer kunder": customers from the business's old system go into Kunder.
// Nobody gets an e-mail or SMS, and no account is made: the business is the data controller of its own list
public class CustomerImportService(AppDbContext dbContext) : ICustomerImportService
{
    // One file at a time; a bigger list is split in more files
    public const int MaxRows = 5000;

    // All imported customers of one business together
    public const int MaxTotal = 20000;

    // The problems the screen shows (the counts include all of them)
    public const int MaxProblems = 50;

    public async Task<(CustomerImportResultDto? Result, string? Error)> ImportAsync(Guid businessId,
        CustomerImportRequest request, CancellationToken cancellationToken = default)
    {
        var rows = request.Rows ?? [];
        if (rows.Count == 0)
        {
            return (null, "Filen har ingen kunder.");
        }

        if (rows.Count > MaxRows)
        {
            return (null, $"Maks {MaxRows} kunder per fil. Del filen i flere og importer dem én om gangen.");
        }

        if (request.Save && !request.Confirmed)
        {
            return (null, "Bekreft at dere har lov til å flytte kundelisten til BESTI.");
        }

        // Customers this business already has: the phone of every booking (an account customer too)
        // and the earlier imports. A phone with an account points to the account's card
        var known = new Dictionary<string, string>();
        var bookings = await dbContext.Bookings.AsNoTracking()
            .Where(b => b.Service.BusinessId == businessId && b.CustomerEmail != BookingAnonymizer.AnonymizedEmail)
            .Select(b => new { b.CustomerUserId, b.CustomerPhone })
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var booking in bookings)
        {
            var digits = CustomerKey.Digits(booking.CustomerPhone);
            var key = CustomerKey.For(booking.CustomerUserId, booking.CustomerPhone);
            if (key is not null && (booking.CustomerUserId is not null || !known.ContainsKey(digits)))
            {
                known[digits] = key;
            }
        }

        var imported = await dbContext.ImportedCustomers.AsNoTracking()
            .Where(c => c.BusinessId == businessId)
            .Select(c => c.PhoneKey)
            .ToListAsync(cancellationToken);
        foreach (var phoneKey in imported)
        {
            known.TryAdd(phoneKey, $"t-{phoneKey}");
        }

        var notedKeys = (await dbContext.CustomerNotes.AsNoTracking()
                .Where(n => n.BusinessId == businessId)
                .Select(n => n.CustomerUserId != null ? "k-" + n.CustomerUserId : "t-" + n.CustomerPhone)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var now = DateTime.UtcNow;
        var problems = new List<CustomerImportProblem>();
        var inFile = new HashSet<string>();
        var newCustomers = new List<ImportedCustomer>();
        var newNotes = new List<CustomerNote>();
        int existing = 0, duplicates = 0, skipped = 0;

        foreach (var row in rows)
        {
            var name = Clean(row.Name);
            var phone = NormalizePhone(row.Phone);
            if (name is null || phone is null)
            {
                skipped++;
                AddProblem(problems, row.Line, name is null
                    ? "Mangler navn."
                    : $"Ugyldig telefonnummer «{Shorten(row.Phone)}».");
                continue;
            }

            if (name.Length > 100)
            {
                name = name[..100].TrimEnd();
            }

            var phoneKey = CustomerKey.Digits(phone);
            if (!inFile.Add(phoneKey))
            {
                duplicates++;
                continue;
            }

            var email = Clean(row.Email);
            if (email is not null && (email.Length > 250 || !email.Contains('@') || email.Contains(' ')))
            {
                AddProblem(problems, row.Line, $"Ugyldig e-post «{Shorten(email)}», kunden importeres uten e-post.");
                email = null;
            }

            string key;
            if (known.TryGetValue(phoneKey, out var knownKey))
            {
                existing++;
                key = knownKey;
            }
            else
            {
                key = $"t-{phoneKey}";
                newCustomers.Add(new ImportedCustomer
                {
                    Id = Guid.NewGuid(),
                    BusinessId = businessId,
                    Name = name,
                    Phone = phone,
                    PhoneKey = phoneKey,
                    Email = email?.ToLowerInvariant(),
                    ImportedUtc = now
                });
            }

            // The old system's note, only where the business has not written its own in BESTI
            var note = Clean(row.Note);
            if (note is not null && notedKeys.Add(key))
            {
                var (userId, notePhone) = CustomerKey.Parse(key)!.Value;
                newNotes.Add(new CustomerNote
                {
                    Id = Guid.NewGuid(),
                    BusinessId = businessId,
                    CustomerUserId = userId,
                    CustomerPhone = notePhone,
                    Text = note.Length > CustomerCardService.NoteMaxLength
                        ? note[..(CustomerCardService.NoteMaxLength - 1)].TrimEnd() + "…"
                        : note,
                    UpdatedUtc = now
                });
            }
        }

        if (imported.Count + newCustomers.Count > MaxTotal)
        {
            return (null, $"Dere kan ha maks {MaxTotal} importerte kunder. Ta kontakt med oss hvis dere trenger flere.");
        }

        if (request.Save && (newCustomers.Count > 0 || newNotes.Count > 0))
        {
            dbContext.ImportedCustomers.AddRange(newCustomers);
            dbContext.CustomerNotes.AddRange(newNotes);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.IsUniqueViolation())
            {
                // The same file sent twice at the same moment: the first one counts
                return (null, "Kundelisten ble endret samtidig et annet sted. Last siden på nytt og prøv igjen.");
            }
        }

        return (new CustomerImportResultDto(request.Save, newCustomers.Count, existing, duplicates, skipped,
            newNotes.Count, problems), null);
    }

    public async Task<bool> RemoveAsync(Guid businessId, string key, CancellationToken cancellationToken = default)
    {
        if (CustomerKey.Parse(key) is not (null, { } phoneKey))
        {
            return false;
        }

        var customer = await dbContext.ImportedCustomers
            .FirstOrDefaultAsync(c => c.BusinessId == businessId && c.PhoneKey == phoneKey, cancellationToken);
        if (customer is null)
        {
            return false;
        }

        // A customer who has booked here stays in Kunder through the bookings, with the note
        var hasBooked = (await dbContext.Bookings.AsNoTracking()
                .Where(b => b.Service.BusinessId == businessId && b.CustomerPhone.Replace(" ", "").EndsWith(phoneKey))
                .Select(b => b.CustomerPhone)
                .ToListAsync(cancellationToken))
            .Any(p => CustomerKey.Digits(p) == phoneKey);
        if (!hasBooked)
        {
            dbContext.CustomerNotes.RemoveRange(await dbContext.CustomerNotes
                .Where(n => n.BusinessId == businessId && n.CustomerPhone == phoneKey)
                .ToListAsync(cancellationToken));
        }

        dbContext.ImportedCustomers.Remove(customer);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    // Exports write phone numbers in many ways: "+47 912 34 567", "912-34-567", "(+47) 91234567",
    // "004791234567", or "4791234567" when Excel made it a number. null = not a phone number
    public static string? NormalizePhone(string? value)
    {
        var phone = new string((value ?? "").Trim().Where(c => c is not ('-' or '(' or ')' or '.' or '/')).ToArray());
        phone = string.Join(' ', phone.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (phone.StartsWith("00"))
        {
            phone = "+" + phone[2..];
        }

        if (!PhoneNumber.IsValid(phone))
        {
            return null;
        }

        var digits = CustomerKey.Digits(phone);
        return digits.Length is >= 8 and <= 15 ? phone : null;
    }

    // Trimmed, inner spaces and line breaks made one space, empty = null
    private static string? Clean(string? value)
    {
        var text = string.Join(' ', (value ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return text.Length == 0 ? null : text;
    }

    private static string Shorten(string? value)
    {
        var text = (value ?? "").Trim();
        return text.Length > 30 ? text[..30] + "…" : text;
    }

    private static void AddProblem(List<CustomerImportProblem> problems, int line, string message)
    {
        if (problems.Count < MaxProblems)
        {
            problems.Add(new CustomerImportProblem(line, message));
        }
    }
}
