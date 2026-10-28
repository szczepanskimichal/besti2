using besti2.Application.Businesses;
using besti2.Application.Common;
using besti2.Application.Email;
using besti2.Application.Geocoding;
using besti2.Application.Images;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Identity;
using besti2.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services;

// webBaseUrl = address of the Next.js site, used in the confirmation link;
// registry = Brønnøysund (null in tests that do not need it);
// adminEmail = where BESTI gets the approval link for a new business (no mail when null);
// imageStorage = where the logo and gallery files are deleted with the business (null in tests that do not need it)
public class BusinessRegistrationService(
    AppDbContext dbContext,
    UserManager<AppUser> userManager,
    IGeocodingService geocoding,
    IEmailService emailService,
    string webBaseUrl,
    IBusinessRegistry? registry = null,
    string? adminEmail = null,
    MailLimiter? mailLimiter = null,
    IImageStorage? imageStorage = null)
    : IBusinessRegistrationService
{
    private readonly MailLimiter limiter = mailLimiter ?? new MailLimiter();

    public async Task<RegisterBusinessResult> RegisterAsync(RegisterBusinessRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Check the input
        if (string.IsNullOrWhiteSpace(request.BusinessName)
            || string.IsNullOrWhiteSpace(request.Address)
            || string.IsNullOrWhiteSpace(request.PostalCode)
            || string.IsNullOrWhiteSpace(request.City)
            || string.IsNullOrWhiteSpace(request.Phone)
            || string.IsNullOrWhiteSpace(request.Email)
            || !request.Email.Contains('@')
            || string.IsNullOrWhiteSpace(request.Password)
            || request.BusinessName.Length > 200
            || request.Address.Length > 200
            || request.Email.Length > 250)
        {
            return new RegisterBusinessResult(false, "Fyll ut alle feltene.");
        }

        if (!PhoneNumber.IsValid(request.Phone))
        {
            return new RegisterBusinessResult(false, "Oppgi et gyldig telefonnummer, f.eks. 912 34 567.");
        }

        if (request.PostalCode is not { Length: 4 } || !request.PostalCode.All(char.IsDigit))
        {
            return new RegisterBusinessResult(false, "Postnummer må være 4 siffer.");
        }

        if (request.OrgNumber is not { Length: 9 } || !request.OrgNumber.All(char.IsDigit))
        {
            return new RegisterBusinessResult(false, "Organisasjonsnummeret må være 9 siffer.");
        }

        if (!OrgNumber.IsValid(request.OrgNumber))
        {
            return new RegisterBusinessResult(false, "Ugyldig organisasjonsnummer. Sjekk at det er skrevet riktig.");
        }

        if (!Enum.TryParse<BusinessCategory>(request.Category, out var category) || !Enum.IsDefined(category))
        {
            return new RegisterBusinessResult(false, "Velg en gyldig kategori.");
        }

        if (!request.AcceptDataProcessing)
        {
            return new RegisterBusinessResult(false, "Du må godta databehandleravtalen for å registrere bedriften.");
        }

        // Only real, active businesses (when the register does not answer, the registration goes on)
        if (registry is not null)
        {
            var lookup = await registry.LookupAsync(request.OrgNumber, cancellationToken);
            if (lookup.Status == RegistryStatus.NotFound)
            {
                return new RegisterBusinessResult(false,
                    "Fant ikke virksomheten i Enhetsregisteret (Brønnøysund). Sjekk organisasjonsnummeret.");
            }

            if (lookup.Status == RegistryStatus.Closed)
            {
                return new RegisterBusinessResult(false,
                    "Virksomheten er slettet, konkurs eller under avvikling i Enhetsregisteret.");
            }
        }

        // Check the password first, so a weak one never starts saving anything
        foreach (var validator in userManager.PasswordValidators)
        {
            var check = await validator.ValidateAsync(userManager, new AppUser(), request.Password);
            if (!check.Succeeded)
            {
                return new RegisterBusinessResult(false,
                    "Passordet må ha minst 6 tegn, stor og liten bokstav, et tall og et spesialtegn.");
            }
        }

        var email = request.Email.Trim().ToLowerInvariant();
        // The login e-mail is never shown: with a public one anyone could lock the owner out with wrong passwords
        var contactEmail = string.IsNullOrWhiteSpace(request.ContactEmail)
            ? null
            : request.ContactEmail.Trim().ToLowerInvariant();
        if (contactEmail is not null && (!contactEmail.Contains('@') || contactEmail.Length > 250))
        {
            return new RegisterBusinessResult(false, "Ugyldig e-post til kunder.");
        }

        if (contactEmail == email)
        {
            return new RegisterBusinessResult(false,
                "Bruk en annen e-post til kunder enn den du logger inn med, eller la feltet stå tomt.");
        }

        // Checked before the old registration is removed below, because that makes the link already sent stop working
        if (!limiter.TryReserve(email))
        {
            return new RegisterBusinessResult(false, MailLimiter.TryLaterMessage);
        }

        // An earlier registration that was never confirmed (lost mail, expired link, or someone else
        // using this org number or e-mail) must not block a new one: it is removed first
        await RemoveUnconfirmedRegistrationsAsync(request.OrgNumber, email, cancellationToken);

        if (await dbContext.Businesses.AnyAsync(b => b.OrgNumber == request.OrgNumber, cancellationToken))
        {
            return new RegisterBusinessResult(false, "Denne bedriften er allerede registrert.");
        }

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return new RegisterBusinessResult(false, "E-postadressen er allerede i bruk.");
        }

        // Map position from the address; the business is saved without it when not found
        var location =
            await geocoding.GeocodeAsync(request.Address.Trim(), request.PostalCode.Trim(), cancellationToken);
        // All or nothing: business, owner account, employee and opening hours are saved together
        // (the in-memory test database has no transactions)
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        // 2. Save the business
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = request.BusinessName.Trim(),
            OrgNumber = request.OrgNumber,
            Category = category,
            Email = contactEmail,
            PhoneNumber = request.Phone.Trim(),
            Address = request.Address.Trim(),
            PostalCode = request.PostalCode.Trim(),
            City = request.City.Trim(),
            Latitude = location?.Latitude,
            Longitude = location?.Longitude,
            IsHidden = true, // customers see it after the owner confirms the email and BESTI approves it
            DataProcessingAcceptedUtc = DateTime.UtcNow
        };
        dbContext.Businesses.Add(business);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            // Two registrations with the same org number at the same moment: the second one stops here
            return new RegisterBusinessResult(false, "Denne bedriften er allerede registrert.");
        }

        // 3. Create the owner account linked to the business
        // The owner must confirm the email before logging in (link in the mail at the end)
        var user = new AppUser { UserName = email, Email = email, BusinessId = business.Id };
        var created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            // Nothing is kept: the transaction is never committed
            return new RegisterBusinessResult(false, "Kunne ikke opprette kontoen. Prøv igjen.");
        }

        // 4. Defaults so the business is bookable right away: owner as employee, Mon-Fri 09-17
        dbContext.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(),
            Name = business.Name,
            LastName = "",
            BusinessId = business.Id
        });

        foreach (var day in new[]
                     { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
        {
            dbContext.OpeningHours.Add(new OpeningHours
            {
                Id = Guid.NewGuid(),
                DayOfWeek = day,
                OpensAt = new TimeOnly(9, 0),
                ClosesAt = new TimeOnly(17, 0),
                BusinessId = business.Id
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        await TrySendConfirmationAsync(user, cancellationToken);
        return new RegisterBusinessResult(true);
    }

    // After the owner has confirmed the email: an approved business becomes visible to customers,
    // a new one waits and BESTI gets a one-click approval link (anyone can type someone else's org number)
    public async Task ShowBusinessOfOwnerAsync(string userId, CancellationToken cancellationToken = default)
    {
        var owner = await userManager.FindByIdAsync(userId);
        if (owner is not { EmailConfirmed: true, BusinessId: not null })
        {
            return;
        }

        var business = await dbContext.Businesses.FindAsync([owner.BusinessId.Value], cancellationToken);
        if (business is not { IsHidden: true })
        {
            return;
        }

        if (business.ApprovedUtc is not null)
        {
            business.IsHidden = false;
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        // Already waiting for BESTI (the owner clicked the link again): the first approval link stays valid
        if (business.ApprovalTokenHash is not null)
        {
            return;
        }

        var token = SecureToken.New();
        business.ApprovalTokenHash = SecureToken.Hash(token);
        await dbContext.SaveChangesAsync(cancellationToken);
        await TrySendApprovalRequestAsync(business, owner.Email!, token, cancellationToken);
    }

    public async Task<BusinessApprovalDto?> GetApprovalAsync(string token,
        CancellationToken cancellationToken = default)
    {
        var business = await FindByApprovalTokenAsync(token, cancellationToken);
        return business is null
            ? null
            : new BusinessApprovalDto(business.Name, business.OrgNumber, business.Category.ToString(), business.Address,
                business.PostalCode, business.City, await OwnerEmailAsync(business.Id, cancellationToken) ?? "",
                business.PhoneNumber);
    }

    // The business shows up for customers and the owner gets a mail; the link works only once
    public async Task<bool> ApproveAsync(string token, CancellationToken cancellationToken = default)
    {
        var business = await FindByApprovalTokenAsync(token, cancellationToken);
        if (business is null)
        {
            return false;
        }

        business.ApprovedUtc = DateTime.UtcNow;
        business.ApprovalTokenHash = null;
        business.IsHidden = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        var body =
            "Hei!\n\n" +
            $"{business.Name} er nå godkjent og synlig for kundene på BESTI.\n" +
            $"Logg inn for å legge til tjenester, ansatte og åpningstider:\n{webBaseUrl}/panel\n\n" +
            "Hilsen BESTI";
        try
        {
            var ownerEmail = await OwnerEmailAsync(business.Id, cancellationToken);
            if (ownerEmail is not null)
            {
                await emailService.SendAsync(ownerEmail, "Bedriften din er godkjent hos BESTI", body,
                    cancellationToken);
            }
        }
        catch (Exception)
        {
            // The approval is saved; the owner also sees it in the panel
        }

        return true;
    }

    // Someone registered an org number that is not theirs: the business and the owner account are
    // deleted, so the real owner can register it, and the person who registered gets a mail
    public async Task<bool> RejectAsync(string token, CancellationToken cancellationToken = default)
    {
        var business = await FindByApprovalTokenAsync(token, cancellationToken);
        if (business is null)
        {
            return false;
        }

        var ownerEmail = await OwnerEmailAsync(business.Id, cancellationToken);
        var body =
            "Hei!\n\n" +
            $"Registreringen av {business.Name} hos BESTI ble avvist, og kontoen er slettet.\n" +
            "Vi kunne ikke bekrefte at du representerer bedriften.\n\n" +
            "Mener du at dette er feil? Kontakt oss på post@bestiapp.no.\n\n" +
            "Hilsen BESTI";
        await DeleteBusinessAsync(business, cancellationToken);
        try
        {
            if (ownerEmail is not null)
            {
                await emailService.SendAsync(ownerEmail, "Registreringen hos BESTI ble avvist", body,
                    cancellationToken);
            }
        }
        catch (Exception)
        {
            // The registration is deleted anyway
        }

        return true;
    }

    // "Slett bedrift" in the panel: the owner confirms with the password. Coming visits must be cancelled
    // first in the calendar, so every customer gets the normal cancel e-mail
    public async Task<string?> DeleteOwnBusinessAsync(string userId, string? password,
        CancellationToken cancellationToken = default)
    {
        var owner = await userManager.FindByIdAsync(userId);
        if (owner?.BusinessId is not Guid businessId)
        {
            return "Fant ikke bedriften.";
        }

        if (!await userManager.CheckPasswordAsync(owner, password ?? ""))
        {
            return "Passordet er feil.";
        }

        var now = DateTime.UtcNow;
        if (await dbContext.Bookings.AnyAsync(b => b.Employee.BusinessId == businessId
                                                   && b.Status != BookingStatus.Avlyst && b.StartUtc > now,
                cancellationToken))
        {
            return "Dere har kommende timer. Avlys dem i kalenderen først, så får kundene beskjed.";
        }

        var business = await dbContext.Businesses.FindAsync([businessId], cancellationToken);
        if (business is null)
        {
            return "Fant ikke bedriften.";
        }

        await DeleteBusinessAsync(business, cancellationToken);
        return null;
    }

    // The owner's login e-mail (private), where BESTI's mails to the business go
    private Task<string?> OwnerEmailAsync(Guid businessId, CancellationToken cancellationToken) =>
        dbContext.Users.Where(u => u.BusinessId == businessId).Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<Business?> FindByApprovalTokenAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 100)
        {
            return null;
        }

        var hash = SecureToken.Hash(token);
        return await dbContext.Businesses.FirstOrDefaultAsync(b => b.ApprovalTokenHash == hash, cancellationToken);
    }

    // To BESTI: who registered, a link to the business in Brønnøysund and the one-click approval link
    private async Task TrySendApprovalRequestAsync(Business business, string ownerEmail, string token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(adminEmail))
        {
            return;
        }

        var body =
            "Ny bedrift venter på godkjenning:\n\n" +
            $"{business.Name} (org.nr. {business.OrgNumber})\n" +
            $"{business.Address}, {business.PostalCode} {business.City}\n" +
            $"Innlogging: {ownerEmail}, e-post til kunder: {business.Email ?? "ingen"}, telefon: {business.PhoneNumber}\n\n" +
            $"Brønnøysund: https://virksomhet.brreg.no/nb/oppslag/enheter/{business.OrgNumber}\n\n" +
            $"Godkjenn: {webBaseUrl}/godkjenn?token={token}\n";
        try
        {
            await emailService.SendAsync(adminEmail, $"Ny bedrift venter på godkjenning: {business.Name}", body,
                cancellationToken);
        }
        catch (Exception)
        {
            // Mail server down: the business waits; it can be approved in the database
        }
    }

    // Mails the confirmation link; a mail error must not undo the registration
    private async Task TrySendConfirmationAsync(AppUser user, CancellationToken cancellationToken)
    {
        var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var link = $"{webBaseUrl}/bekreft-epost?id={Uri.EscapeDataString(user.Id)}&kode={Uri.EscapeDataString(code)}";
        var body =
            "Hei!\n\n" +
            // No business name from the form in the mail: it goes to an address nobody has confirmed yet
            "Takk for at du registrerte bedriften din hos BESTI.\n" +
            $"Klikk på lenken for å bekrefte e-postadressen, så kan du logge inn:\n{link}\n\n" +
            "Har du ikke registrert en bedrift, kan du se bort fra denne e-posten.\n\n" +
            "Hilsen BESTI";
        try
        {
            await emailService.SendAsync(user.Email!, "Bekreft e-postadressen til bedriften din hos BESTI", body,
                cancellationToken);
        }
        catch (Exception)
        {
            // Mail server down: the business is saved, the link can be sent again later
        }
    }

    // A hidden business whose owner never confirmed the e-mail, found by org number or by the owner's
    // e-mail, is deleted together with its owner. A confirmed owner is never touched.
    private async Task RemoveUnconfirmedRegistrationsAsync(string orgNumber, string email,
        CancellationToken cancellationToken)
    {
        var businessIds = await dbContext.Businesses
            .Where(b => b.IsHidden && b.OrgNumber == orgNumber)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);
        if (await userManager.FindByEmailAsync(email) is { EmailConfirmed: false, BusinessId: Guid ownBusinessId })
        {
            businessIds.Add(ownBusinessId);
        }

        foreach (var businessId in businessIds.Distinct())
        {
            var business = await dbContext.Businesses.FindAsync([businessId], cancellationToken);
            if (business is not { IsHidden: true }
                || await dbContext.Users.AnyAsync(u => u.BusinessId == businessId && u.EmailConfirmed,
                    cancellationToken))
            {
                continue;
            }

            await DeleteBusinessAsync(business, cancellationToken);
        }
    }

    // Deletes a business with its owner and everything added in the panel (a rejected registration or
    // "Slett bedrift"). Bookings, reviews and time off are removed by hand first, because
    // the database does not delete them on its own (Restrict). All or nothing: never half a business
    private async Task DeleteBusinessAsync(Business business, CancellationToken cancellationToken)
    {
        // The in-memory test database has no transactions
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        var bookings = await dbContext.Bookings.Where(b => b.Employee.BusinessId == business.Id)
            .ToListAsync(cancellationToken);
        var bookingIds = bookings.Select(b => b.Id).ToList();
        var imageFiles = await dbContext.BusinessImages.Where(i => i.BusinessId == business.Id)
            .Select(i => i.FileName)
            .ToListAsync(cancellationToken);
        dbContext.Reviews.RemoveRange(await dbContext.Reviews.Where(r => r.BusinessId == business.Id)
            .ToListAsync(cancellationToken));
        dbContext.Bookings.RemoveRange(bookings);
        dbContext.TimeOffs.RemoveRange(await dbContext.TimeOffs.Where(t => t.BusinessId == business.Id)
            .ToListAsync(cancellationToken));
        // The business's bell, and the customers' bells about these visits
        dbContext.Notifications.RemoveRange(await dbContext.Notifications
            .Where(n => n.BusinessId == business.Id || (n.BookingId != null && bookingIds.Contains(n.BookingId.Value)))
            .ToListAsync(cancellationToken));
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var owner in await dbContext.Users.Where(u => u.BusinessId == business.Id)
                     .ToListAsync(cancellationToken))
        {
            var deleted = await userManager.DeleteAsync(owner);
            if (!deleted.Succeeded)
            {
                // The transaction is never committed: everything above is undone
                throw new InvalidOperationException($"Could not delete the owner of business {business.Id}");
            }
        }

        dbContext.Businesses.Remove(business);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        // The files only after the database no longer points to them; a file left behind is no reason to fail
        if (imageStorage is null)
        {
            return;
        }

        foreach (var fileName in imageFiles)
        {
            try
            {
                await imageStorage.DeleteAsync(fileName, cancellationToken);
            }
            catch (Exception)
            {
                // The business is deleted anyway
            }
        }
    }
}