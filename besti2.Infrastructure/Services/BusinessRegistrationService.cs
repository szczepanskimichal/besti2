using besti2.Application.Businesses;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Identity;
using besti2.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services;

public class BusinessRegistrationService(AppDbContext dbContext, UserManager<AppUser> userManager) : IBusinessRegistrationService
{
    public async Task<RegisterBusinessResult> RegisterAsync(RegisterBusinessRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Check the input
        if (string.IsNullOrWhiteSpace(request.BusinessName) || string.IsNullOrWhiteSpace(request.Address)
            || string.IsNullOrWhiteSpace(request.PostalCode) || string.IsNullOrWhiteSpace(request.City)
            || string.IsNullOrWhiteSpace(request.Phone) || string.IsNullOrWhiteSpace(request.Email)
            || !request.Email.Contains('@') || string.IsNullOrWhiteSpace(request.Password)
            || request.BusinessName.Length > 200 || request.Phone.Length > 20 || request.Email.Length > 250)
        
            if (request.PostalCode is not { Length: 4 } || !request.PostalCode.All(char.IsDigit))
            {
                return new RegisterBusinessResult(false, "Postnummer må være 4 siffer.");
            }

        if (request.OrgNumber is not { Length: 9 } || !request.OrgNumber.All(char.IsDigit))        {
            return new RegisterBusinessResult(false, "Organisasjonsnummeret må være 9 siffer.");
        }

        if (!Enum.TryParse<BusinessCategory>(request.Category, out var category) || !Enum.IsDefined(category))
        {
            return new RegisterBusinessResult(false, "Velg en gyldig kategori.");
        }

        var email = request.Email.Trim().ToLowerInvariant();

        if (await dbContext.Businesses.AnyAsync(b => b.OrgNumber == request.OrgNumber, cancellationToken))
        {
            return new RegisterBusinessResult(false, "Denne bedriften er allerede registrert.");
        }

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return new RegisterBusinessResult(false, "E-postadressen er allerede i bruk.");
        }

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
            City = request.City.Trim()
        };
        dbContext.Businesses.Add(business);
        await dbContext.SaveChangesAsync(cancellationToken);

        // 3. Create the owner account linked to the business
        var user = new AppUser { UserName = email, Email = email, BusinessId = business.Id };
        var created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            // No owner, no business: remove it again
            dbContext.Businesses.Remove(business);
            await dbContext.SaveChangesAsync(cancellationToken);
            return new RegisterBusinessResult(false, "Passordet må ha minst 6 tegn, stor og liten bokstav, et tall og et spesialtegn.");
        }

        return new RegisterBusinessResult(true);
    }
}