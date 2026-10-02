using besti2.Application.Businesses;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services;

public class BusinessService(AppDbContext dbContext) : IBusinessService
{
    public async Task<IReadOnlyList<BusinessListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Businesses
            .AsNoTracking() // Disable change tracking for better performance!!!
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
}