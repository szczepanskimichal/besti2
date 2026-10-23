using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Persistence;

// The three demo businesses come from the migrations (HasData), so every new database gets them.
// On bestiapp.no they must not exist: customers would see fake businesses and could book there.
public static class DemoData
{
    public static readonly Guid[] BusinessIds =
    [
        new("11111111-1111-1111-1111-111111111111"), // Klipp & Stil Frisør
        new("22222222-2222-2222-2222-222222222222"), // Fjord Bygg AS
        new("33333333-3333-3333-3333-333333333333"), // Nordlys Psykologsenter
    ];

    // Safe to run on every start: does nothing once they are gone. Returns how many businesses were removed.
    public static async Task<int> RemoveAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var businesses = await dbContext.Businesses
            .Where(b => BusinessIds.Contains(b.Id))
            .ToListAsync(cancellationToken);
        if (businesses.Count == 0)
        {
            return 0;
        }

        dbContext.OpeningHours.RemoveRange(
            await dbContext.OpeningHours.Where(h => BusinessIds.Contains(h.BusinessId)).ToListAsync(cancellationToken));
        dbContext.Employees.RemoveRange(
            await dbContext.Employees.Where(e => BusinessIds.Contains(e.BusinessId)).ToListAsync(cancellationToken));
        dbContext.Services.RemoveRange(
            await dbContext.Services.Where(s => BusinessIds.Contains(s.BusinessId)).ToListAsync(cancellationToken));
        dbContext.Businesses.RemoveRange(businesses);
        await dbContext.SaveChangesAsync(cancellationToken);
        return businesses.Count;
    }
}