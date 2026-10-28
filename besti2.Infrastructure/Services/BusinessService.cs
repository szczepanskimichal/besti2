using System.Linq.Expressions;
using besti2.Application.Businesses;
using besti2.Application.Common;
using besti2.Application.Geocoding;
using besti2.Application.Images;
using besti2.Domain.Enums;
using besti2.Domain.Entities;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services;

public class BusinessService(AppDbContext dbContext, IGeocodingService geocoding, IImageStorage imageStorage)
    : IBusinessService
{
    // The database keeps only the file name; the storage knows the public address
    private string? ToLogoUrl(string? fileName) => fileName is null ? null : imageStorage.GetUrl(fileName);

    public Task<BusinessDetailsDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetDetailsAsync(b => b.Id == id, false, cancellationToken);

    public Task<BusinessDetailsDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        BusinessSlug.IsValid(slug)
            ? GetDetailsAsync(b => b.Slug == slug, false, cancellationToken)
            : Task.FromResult<BusinessDetailsDto?>(null);

    public Task<BusinessDetailsDto?> GetForPanelAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetDetailsAsync(b => b.Id == id, true, cancellationToken);

    private async Task<BusinessDetailsDto?> GetDetailsAsync(Expression<Func<Business, bool>> match, bool includeHidden,
        CancellationToken cancellationToken)
    {
        var business = await dbContext.Businesses
            // Services, employees, hours and photos as separate small queries instead of one huge joined one
            .AsSplitQuery()
            .AsNoTracking()
            .Where(match)
            .Where(b => includeHidden || !b.IsHidden)
            .Select(b => new BusinessDetailsDto(
                b.Id,
                b.Slug,
                b.Name,
                b.Category.ToString(),
                b.Description,
                b.Email,
                b.PhoneNumber,
                b.Address,
                b.PostalCode,
                b.City,
                b.Services
                    .Where(s => !s.IsDeleted)
                    .OrderBy(s => s.Name)
                    .Select(s => new ServiceDto(
                        s.Id, s.Name, s.Description, s.DurationMinutes, s.PriceNok, s.PriceType.ToString()))
                    .ToList(),
                b.Employees
                    .Where(e => !e.IsDeleted)
                    .OrderBy(e => e.Name)
                    .Select(e => new EmployeeDto(e.Id, e.Name, e.LastName))
                    .ToList(),
                b.OpeningHours
                    .OrderBy(h => h.DayOfWeek)
                    .Select(h => new OpeningHoursDto(h.DayOfWeek, h.OpensAt, h.ClosesAt))
                    .ToList(),
                b.Latitude,
                b.Longitude,
                // Logo file name for now; turned into a URL below
                b.Images.Where(i => i.Kind == ImageKind.Logo).Select(i => i.FileName).FirstOrDefault(),
                // File names for now; turned into URLs below
                b.Images.Where(i => i.Kind == ImageKind.Galleri)
                    .OrderBy(i => i.SortOrder).ThenBy(i => i.CreatedUtc)
                    .Select(i => new GalleryImageDto(i.Id, i.FileName))
                    .ToList(),
                b.IsHidden,
                b.FreeCancelHours,
                b.Website,
                b.AutoConfirm))
            .FirstOrDefaultAsync(cancellationToken);

        return business is null
            ? null
            : business with
            {
                LogoUrl = ToLogoUrl(business.LogoUrl),
                Gallery = business.Gallery.Select(g => g with { Url = imageStorage.GetUrl(g.Url) }).ToList()
            };
    }

    private const decimal MaxPriceNok = 1_000_000m;

    public async Task<string?> AddServiceAsync(Guid businessId, CreateServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        var error = ValidateService(request);
        if (error is not null)
        {
            return error;
        }

        var priceType = Enum.Parse<PriceType>(request.PriceType);

        dbContext.Services.Add(new Service
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            DurationMinutes = request.DurationMinutes,
            PriceType = priceType,
            PriceNok = priceType == PriceType.EtterAvtale ? null : request.PriceNok
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return null;
    }

    public async Task<string?> UpdateServiceAsync(Guid businessId, Guid serviceId, CreateServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        var error = ValidateService(request);
        if (error is not null)
        {
            return error;
        }

        var service = await dbContext.Services.FirstOrDefaultAsync(
            s => s.Id == serviceId && s.BusinessId == businessId && !s.IsDeleted, cancellationToken);
        if (service is null)
        {
            return "Fant ikke tjenesten.";
        }

        var priceType = Enum.Parse<PriceType>(request.PriceType);
        service.Name = request.Name.Trim();
        service.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        service.DurationMinutes = request.DurationMinutes;
        service.PriceType = priceType;
        service.PriceNok = priceType == PriceType.EtterAvtale ? null : request.PriceNok;

        await dbContext.SaveChangesAsync(cancellationToken);
        return null;
    }

    public async Task<bool> DeleteServiceAsync(Guid businessId, Guid serviceId,
        CancellationToken cancellationToken = default)
    {
        var service = await dbContext.Services.FirstOrDefaultAsync(
            s => s.Id == serviceId && s.BusinessId == businessId && !s.IsDeleted, cancellationToken);
        if (service is null)
        {
            return false;
        }

        service.IsDeleted = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<string?> AddEmployeeAsync(Guid businessId, EmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var error = ValidateEmployee(request);

        if (error is not null)
        {
            return error;
        }

        dbContext.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Name = request.Name.Trim(),
            LastName = request.LastName?.Trim() ?? ""
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return null;
    }

    public async Task<string?> UpdateEmployeeAsync(Guid businessId, Guid employeeId, EmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var error = ValidateEmployee(request);

        if (error is not null)
        {
            return error;
        }

        var employee = await dbContext.Employees.FirstOrDefaultAsync(
            e => e.Id == employeeId && e.BusinessId == businessId && !e.IsDeleted, cancellationToken);

        if (employee is null)
        {
            return "Fant ikke den ansatte.";
        }

        employee.Name = request.Name.Trim();

        employee.LastName = request.LastName?.Trim() ?? "";

        await dbContext.SaveChangesAsync(cancellationToken);

        return null;
    }

    public async Task<string?> DeleteEmployeeAsync(Guid businessId, Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.FirstOrDefaultAsync(
            e => e.Id == employeeId && e.BusinessId == businessId && !e.IsDeleted, cancellationToken);
        if (employee is null)
        {
            return "Fant ikke den ansatte.";
        }

        // Without employees nobody can book, so the last one must stay
        var activeCount = await dbContext.Employees
            .CountAsync(e => e.BusinessId == businessId && !e.IsDeleted, cancellationToken);
        if (activeCount <= 1)
        {
            return "Bedriften må ha minst én ansatt.";
        }

        // Visits still ahead would disappear from the calendar with the employee: move them first
        var now = DateTime.UtcNow;
        var upcoming = await dbContext.Bookings
            .CountAsync(b => b.EmployeeId == employeeId && b.StartUtc > now
                             && b.Status != BookingStatus.Avlyst && b.Status != BookingStatus.MotteIkke,
                cancellationToken);
        if (upcoming > 0)
        {
            return $"{employee.Name} har {upcoming} kommende {(upcoming == 1 ? "time" : "timer")}. Flytt dem først.";
        }

        employee.IsDeleted = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return null;
    }

    public async Task<string?> UpdateProfileAsync(Guid businessId, UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Phone)
            || string.IsNullOrWhiteSpace(request.Address)
            || string.IsNullOrWhiteSpace(request.City)
            || request.Address.Length > 200 || request.City.Length > 200)
        {
            return "Fyll ut telefon, adresse og by.";
        }

        if (!PhoneNumber.IsValid(request.Phone))
        {
            return "Oppgi et gyldig telefonnummer, f.eks. 912 34 567.";
        }

        if (request.PostalCode is not { Length: 4 } || !request.PostalCode.All(char.IsDigit))
        {
            return "Postnummer må være 4 siffer.";
        }

        if (request.Description is { Length: > 1000 })
        {
            return "Beskrivelsen kan ha maks 1000 tegn.";
        }

        if (!Business.FreeCancelOptions.Contains(request.FreeCancelHours))
        {
            return "Velg en gyldig frist for gratis avbestilling.";
        }

        var contactEmail = string.IsNullOrWhiteSpace(request.ContactEmail)
            ? null
            : request.ContactEmail.Trim().ToLowerInvariant();
        if (contactEmail is not null && (!contactEmail.Contains('@') || contactEmail.Length > 250))
        {
            return "Ugyldig e-post til kunder.";
        }

        var website = ToWebsite(request.Website);
        if (website is { Length: 0 })
        {
            return "Ugyldig nettside. Skriv den f.eks. slik: klippogstil.no";
        }

        var business = await dbContext.Businesses.FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);
        if (business is null)
        {
            return "Fant ikke bedriften.";
        }

        // The login e-mail must stay private (a public one lets anyone lock the owner out)
        var normalizedContact = contactEmail?.ToUpperInvariant();
        if (normalizedContact is not null && await dbContext.Users.AnyAsync(
                u => u.BusinessId == businessId && u.NormalizedEmail == normalizedContact, cancellationToken))
        {
            return "Bruk en annen e-post til kunder enn den du logger inn med, eller la feltet stå tomt.";
        }

        var addressChanged = business.Address != request.Address.Trim() || business.PostalCode != request.PostalCode;
        business.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        business.PhoneNumber = request.Phone.Trim();
        business.Address = request.Address.Trim();
        business.PostalCode = request.PostalCode;
        business.City = request.City.Trim();
        business.Email = contactEmail;
        business.FreeCancelHours = request.FreeCancelHours;
        business.Website = website;
        business.AutoConfirm = request.AutoConfirm;
        // New address: look up the map position again (null = no map)
        if (addressChanged)
        {
            var point = await geocoding.GeocodeAsync(business.Address, business.PostalCode, cancellationToken);
            business.Latitude = point?.Latitude;
            business.Longitude = point?.Longitude;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return null;
    }

    public async Task<string?> UpdateOpeningHoursAsync(Guid businessId, UpdateOpeningHoursRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Days is null)
        {
            return "Ugyldige dager.";
        }

        if (request.Days.Any(d => !Enum.IsDefined(d.DayOfWeek))
            || request.Days.Select(d => d.DayOfWeek).Distinct().Count() != request.Days.Count)
        {
            return "Ugyldige dager.";
        }

        if (request.Days.Any(d => d.ClosesAt <= d.OpensAt))
        {
            return "Stengetid må være etter åpningstid.";
        }

        var existing = await dbContext.OpeningHours
            .Where(h => h.BusinessId == businessId)
            .ToListAsync(cancellationToken);

        // Days not in the request become closed
        dbContext.OpeningHours.RemoveRange(
            existing.Where(h => request.Days.All(d => d.DayOfWeek != h.DayOfWeek)));

        foreach (var day in request.Days)
        {
            var row = existing.FirstOrDefault(h => h.DayOfWeek == day.DayOfWeek);
            if (row is null)
            {
                row = new OpeningHours { Id = Guid.NewGuid(), BusinessId = businessId, DayOfWeek = day.DayOfWeek };
                dbContext.OpeningHours.Add(row);
            }

            row.OpensAt = day.OpensAt;
            row.ClosesAt = day.ClosesAt;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return null;
    }

    // Shared validation for adding and editing a service; returns null when valid
    private static string? ValidateService(CreateServiceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200)
        {
            return "Fyll ut navn på tjenesten (maks 200 tegn).";
        }

        if (request.DurationMinutes < 5 || request.DurationMinutes > 480)
        {
            return "Varighet må være mellom 5 og 480 minutter.";
        }

        if (!Enum.TryParse<PriceType>(request.PriceType, out var priceType) || !Enum.IsDefined(priceType))
        {
            return "Velg en gyldig pristype.";
        }

        if (request.PriceNok is < 0)
        {
            return "Prisen kan ikke være negativ.";
        }

        // The database column holds at most 99 999 999,99; no real service costs a million
        if (request.PriceNok is > MaxPriceNok)
        {
            return "Prisen kan være maks 1 000 000 kr.";
        }

        return null;
    }

    // null = no website; "" = not a valid address. Only http and https (never javascript: and the like)
    private static string? ToWebsite(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var text = input.Trim();
        if (!text.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !text.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            text = "https://" + text;
        }

        return text.Length <= 200
               && Uri.TryCreate(text, UriKind.Absolute, out var uri)
               && uri.Scheme is "http" or "https"
               && uri.Host.Contains('.')
            ? text
            : "";
    }

    // Shared validation for adding and editing an employee; returns null when valid
    private static string? ValidateEmployee(EmployeeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200)
        {
            return "Fyll ut navn på den ansatte (maks 200 tegn).";
        }

        if (request.LastName is { Length: > 200 })
        {
            return "Etternavnet kan være maks 200 tegn.";
        }

        return null;
    }
}