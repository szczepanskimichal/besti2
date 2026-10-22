using besti2.Application.Businesses;
using besti2.Application.Email;
using besti2.Application.Geocoding;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Identity;
using besti2.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services;

// webBaseUrl = address of the Next.js site, used in the confirmation link
// webBaseUrl = address of the Next.js site, used in the confirmation link;
// registry = Brønnøysund (null in tests that do not need it)
public class BusinessRegistrationService(AppDbContext dbContext, UserManager<AppUser> userManager,
    IGeocodingService geocoding, IEmailService emailService, string webBaseUrl, IBusinessRegistry? registry = null)
    : IBusinessRegistrationService{
    public async Task<RegisterBusinessResult> RegisterAsync(RegisterBusinessRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Check the input
        if (string.IsNullOrWhiteSpace(request.BusinessName) || string.IsNullOrWhiteSpace(request.Address)
            || string.IsNullOrWhiteSpace(request.PostalCode) || string.IsNullOrWhiteSpace(request.City)
            || string.IsNullOrWhiteSpace(request.Phone) || string.IsNullOrWhiteSpace(request.Email)
            || !request.Email.Contains('@') || string.IsNullOrWhiteSpace(request.Password)
            || request.BusinessName.Length > 200 || request.Phone.Length > 20 || request.Email.Length > 250)
        {
            return new RegisterBusinessResult(false, "Fyll ut alle feltene.");
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
        var location = await geocoding.GeocodeAsync(request.Address.Trim(), request.PostalCode.Trim(), cancellationToken);
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
            Email = email,
            PhoneNumber = request.Phone.Trim(),
            Address = request.Address.Trim(),
            PostalCode = request.PostalCode.Trim(),
            City = request.City.Trim(),
            Latitude = location?.Latitude,
            Longitude = location?.Longitude,
            IsHidden = true // customers see it after the owner confirms the email            
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

        foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
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
    
    
    // After the owner has confirmed the email, the business becomes visible to customers
    public async Task ShowBusinessOfOwnerAsync(string userId, CancellationToken cancellationToken = default)
    {
        var owner = await userManager.FindByIdAsync(userId);
        if (owner is not { EmailConfirmed: true, BusinessId: not null })
        {
            return;
        }

        var business = await dbContext.Businesses.FindAsync([owner.BusinessId.Value], cancellationToken);
        if (business is { IsHidden: true })
        {
            business.IsHidden = false;
            await dbContext.SaveChangesAsync(cancellationToken);
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
            await emailService.SendAsync(user.Email!, "Bekreft e-postadressen til bedriften din hos BESTI", body, cancellationToken);
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
            var owners = await dbContext.Users.Where(u => u.BusinessId == businessId).ToListAsync(cancellationToken);
            var business = await dbContext.Businesses.FindAsync([businessId], cancellationToken);
            if (business is not { IsHidden: true } || owners.Any(u => u.EmailConfirmed))
            {
                continue;
            }
            foreach (var owner in owners)
            {
                await userManager.DeleteAsync(owner);
            }
            dbContext.Businesses.Remove(business);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}