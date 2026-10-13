using besti2.Application.Businesses;
using besti2.Domain.Enums;
using besti2.Domain.Entities;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services;
public class BusinessService(AppDbContext dbContext) : IBusinessService
{
public async Task<IReadOnlyList<BusinessListItemDto>> GetAllAsync(
    string? category = null, string? city = null, CancellationToken cancellationToken = default)
{
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
        var trimmedCity = city.Trim();
        query = query.Where(b => b.City == trimmedCity);
    }

    return await query
        .OrderBy(b => b.Name)
            .Select(b => new BusinessListItemDto(
                b.Id,
                b.Name,
                b.Category.ToString(),
                b.Description,
                b.City
            ))
            .ToListAsync(cancellationToken);
    }


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
        return await dbContext.Businesses
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
                    .OrderBy(s => s.Name)
                    .Select(s => new ServiceDto(
                        s.Id, s.Name, s.Description, s.DurationMinutes, s.PriceNok, s.PriceType.ToString()))
                    .ToList(),
                b.Employees
                    .OrderBy(e => e.Name)
                    .Select(e => new EmployeeDto(e.Id, e.Name, e.LastName))
                    .ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
    
    public async Task<string?> AddServiceAsync(Guid businessId, CreateServiceRequest request,
        CancellationToken cancellationToken = default)
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
}