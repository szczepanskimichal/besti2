using besti2.Application.Bookings;
using besti2.Application.Businesses;
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
    public async Task<IReadOnlyList<BusinessListItemDto>> GetAllAsync(
        string? category = null, string? city = null, string? q = null, DateOnly? date = null,
        CancellationToken cancellationToken = default)    {
        var query = dbContext.Businesses.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(category))
        {
            if (!Enum.TryParse<BusinessCategory>(category, ignoreCase: true, out var parsedCategory)
                || !Enum.IsDefined(parsedCategory))
            {
                return [];
            }

            query = query.Where(b => b.Category == parsedCategory);
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var trimmedCity = city.Trim().ToLower();
            query = query.Where(b => b.City.ToLower().Contains(trimmedCity));
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var text = q.Trim().ToLower();
            query = query.Where(b => b.Name.ToLower().Contains(text)
                                     || b.Services.Any(s => !s.IsDeleted && s.Name.ToLower().Contains(text)));
        }

        var items = await query
            .OrderBy(b => b.Name)
            .Select(b => new BusinessListItemDto(
                b.Id,
                b.Name,
                b.Category.ToString(),
                b.Description,
                b.City,
                b.Images.Where(i => i.Kind == ImageKind.Logo).Select(i => i.FileName).FirstOrDefault(),
                null
            ))
            .ToListAsync(cancellationToken);

        var withLogos = items.Select(b => b with { LogoUrl = ToLogoUrl(b.LogoUrl) }).ToList();

    // Without a date: the list as before. With a date: only businesses with a free time that day

        return date is null ? withLogos : await AddFreeSlotsAsync(withLogos, date.Value, q, cancellationToken);
    }

    // Free times for the whole list with 4 queries, however many businesses there are (no query per business)

    private async Task<IReadOnlyList<BusinessListItemDto>> AddFreeSlotsAsync(
        List<BusinessListItemDto> businesses, DateOnly date, string? q, CancellationToken cancellationToken)

    {
        var businessIds = businesses.Select(b => b.Id).ToList();

        var services = await dbContext.Services.AsNoTracking()
            .Where(s => businessIds.Contains(s.BusinessId) && !s.IsDeleted)
            .Select(s => new { s.Id, s.BusinessId, s.Name, s.DurationMinutes })
            .ToListAsync(cancellationToken);

        var employees = await dbContext.Employees.AsNoTracking()
            .Where(e => businessIds.Contains(e.BusinessId) && !e.IsDeleted)
            .Select(e => new { e.Id, e.BusinessId })
            .ToListAsync(cancellationToken);

        var hours = await dbContext.OpeningHours.AsNoTracking()
            .Where(h => businessIds.Contains(h.BusinessId) && h.DayOfWeek == date.DayOfWeek)
            .ToListAsync(cancellationToken);

        // Same wide UTC window as BookingService.GetFreeSlotsAsync (the whole Oslo day is inside it)

        var fromUtc = date.ToDateTime(TimeOnly.MinValue).AddDays(-1);

        var toUtc = date.ToDateTime(TimeOnly.MinValue).AddDays(2);

        var employeeIds = employees.Select(e => e.Id).ToList();

        var bookings = await dbContext.Bookings.AsNoTracking()
            .Where(b => employeeIds.Contains(b.EmployeeId)
                        && b.Status != BookingStatus.Avlyst
                        && b.StartUtc < toUtc
                        && b.EndUtc > fromUtc)
            .Select(b => new { b.EmployeeId, b.StartUtc, b.EndUtc })
            .ToListAsync(cancellationToken);

        var nowUtc = DateTime.UtcNow;

        var text = q?.Trim().ToLower();

        var result = new List<BusinessListItemDto>();

        foreach (var business in businesses)

        {
            var dayHours = hours.FirstOrDefault(h => h.BusinessId == business.Id);

            var businessServices = services.Where(s => s.BusinessId == business.Id).ToList();

            // The searched service ("klipp" -> Herreklipp), otherwise the shortest one

            var service = businessServices
                              .Where(s => !string.IsNullOrEmpty(text) && s.Name.ToLower().Contains(text))
                              .OrderBy(s => s.DurationMinutes)
                              .FirstOrDefault()
                          ?? businessServices.OrderBy(s => s.DurationMinutes).FirstOrDefault();

            if (dayHours is null || service is null)

            {
                continue; // closed that day, or no services
            }

            var slots = employees
                .Where(e => e.BusinessId == business.Id)
                .SelectMany(e => SlotCalculator.GetFreeSlots(
                        date, dayHours.OpensAt, dayHours.ClosesAt, service.DurationMinutes,
                        bookings.Where(b => b.EmployeeId == e.Id).Select(b => (b.StartUtc, b.EndUtc)).ToList(),
                        nowUtc)
                    .Select(start => new FreeSlotDto(start, service.Id, service.Name, e.Id)))
                .OrderBy(s => s.StartUtc)
                .DistinctBy(s => s.StartUtc) // two free employees at 10:00 = one button
                .Take(3)
                .ToList();
            if (slots.Count > 0)
            {
                result.Add(business with { FreeSlots = slots });
            }
        }
    // Soonest free time first
        return result.OrderBy(b => b.FreeSlots![0].StartUtc).ToList();
    }


    // The database keeps only the file name; the storage knows the public address
    private string? ToLogoUrl(string? fileName) => fileName is null ? null : imageStorage.GetUrl(fileName);

    public async Task<IReadOnlyList<string>> GetCitiesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Businesses
            .AsNoTracking()
            .Select(b => b.City)
            .Distinct()
            .OrderBy(city => city)
            .ToListAsync(cancellationToken);
    }

    public async Task<BusinessDetailsDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var business = await dbContext.Businesses
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new BusinessDetailsDto(
                b.Id,
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
                b.Images.Where(i => i.Kind == ImageKind.Logo).Select(i => i.FileName).FirstOrDefault()
            ))
            .FirstOrDefaultAsync(cancellationToken);

        return business is null ? null : business with { LogoUrl = ToLogoUrl(business.LogoUrl) };
    }

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
            || request.Phone.Length > 20 || request.City.Length > 200)
        {
            return "Fyll ut telefon, adresse og by.";
        }

        if (request.PostalCode is not { Length: 4 } || !request.PostalCode.All(char.IsDigit))
        {
            return "Postnummer må være 4 siffer.";
        }

        if (request.Description is { Length: > 1000 })
        {
            return "Beskrivelsen kan ha maks 1000 tegn.";
        }

        var business = await dbContext.Businesses.FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);
        if (business is null)
        {
            return "Fant ikke bedriften.";
        }

        var addressChanged = business.Address != request.Address.Trim() || business.PostalCode != request.PostalCode;
        business.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        business.PhoneNumber = request.Phone.Trim();
        business.Address = request.Address.Trim();
        business.PostalCode = request.PostalCode;
        business.City = request.City.Trim();
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

        return null;
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