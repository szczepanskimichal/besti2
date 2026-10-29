using besti2.Application.Loyalty;
using besti2.Domain.Entities;
using besti2.Domain.Enums;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services;

// "Stempelkort": the stamps are not stored, they are the customer's visits that took place since the card
// started, minus a full card for every reward given. Visits the business adds itself have no customer
// account and never count
public class LoyaltyService(AppDbContext dbContext) : ILoyaltyService
{
    public const int RewardMaxLength = 100;

    public async Task<PanelLoyaltyDto?> GetForPanelAsync(Guid businessId, DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var business = await dbContext.Businesses.AsNoTracking()
            .Where(b => b.Id == businessId)
            .Select(b => new { b.LoyaltyVisits, b.LoyaltyReward, b.LoyaltyStartUtc })
            .FirstOrDefaultAsync(cancellationToken);
        if (business is null)
        {
            return null;
        }

        if (business.LoyaltyVisits is not int visits || business.LoyaltyStartUtc is not DateTime startUtc)
        {
            return new PanelLoyaltyDto(null, business.LoyaltyReward, null, []);
        }

        var stamps = await VisitsAsync(dbContext.Bookings.Where(b => b.Service.BusinessId == businessId),
            startUtc, nowUtc, cancellationToken);
        var given = await GivenAsync(dbContext.LoyaltyRedemptions.Where(r => r.BusinessId == businessId), startUtc,
            cancellationToken);
        var customers = stamps
            .Select(s =>
            {
                var rewards = given.GetValueOrDefault((businessId, s.UserId));
                var left = s.Count - rewards * visits;
                return new LoyaltyCustomerDto(s.UserId, s.Name, Math.Clamp(left, 0, visits), left >= visits,
                    s.LastVisitUtc, rewards);
            })
            .Where(c => c.Stamps > 0 || c.RewardsGiven > 0)
            .OrderByDescending(c => c.RewardReady)
            .ThenByDescending(c => c.Stamps)
            .ThenByDescending(c => c.LastVisitUtc)
            .ToList();
        return new PanelLoyaltyDto(visits, business.LoyaltyReward, startUtc, customers);
    }

    public async Task<string?> SetAsync(Guid businessId, SetLoyaltyRequest request, DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var business = await dbContext.Businesses.FindAsync([businessId], cancellationToken);
        if (business is null)
        {
            return "Fant ikke bedriften.";
        }

        if (request.Visits is null)
        {
            business.LoyaltyVisits = null;
            business.LoyaltyStartUtc = null;
            await dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }

        var reward = request.Reward?.Trim() ?? "";
        if (!Business.LoyaltyOptions.Contains(request.Visits.Value))
        {
            return "Velg hvor mange besøk.";
        }

        if (reward.Length is 0 or > RewardMaxLength)
        {
            return $"Skriv hva kunden får (maks {RewardMaxLength} tegn), f.eks. «50 % på neste klipp».";
        }

        // Changing the number or the reward keeps the stamps; turning the card on again starts from zero
        business.LoyaltyStartUtc ??= nowUtc;
        business.LoyaltyVisits = request.Visits;
        business.LoyaltyReward = reward;
        await dbContext.SaveChangesAsync(cancellationToken);
        return null;
    }

    public async Task<string?> RedeemAsync(Guid businessId, string customerUserId, DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var card = await GetForPanelAsync(businessId, nowUtc, cancellationToken);
        if (card?.Visits is null)
        {
            return "Stempelkortet er ikke slått på.";
        }

        if (card.Customers.FirstOrDefault(c => c.CustomerUserId == customerUserId) is not { RewardReady: true })
        {
            return "Kunden har ikke fullt stempelkort ennå.";
        }

        dbContext.LoyaltyRedemptions.Add(new LoyaltyRedemption
        {
            Id = Guid.NewGuid(), BusinessId = businessId, CustomerUserId = customerUserId, RedeemedUtc = nowUtc
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return null;
    }

    public async Task<IReadOnlyList<LoyaltyCardDto>> GetMineAsync(string userId, DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var visits = await dbContext.Bookings.AsNoTracking()
            .Where(b => b.CustomerUserId == userId && b.Status == BookingStatus.Bekreftet && b.EndUtc <= nowUtc
                        && b.Service.Business.LoyaltyVisits != null && !b.Service.Business.IsHidden
                        && b.StartUtc >= b.Service.Business.LoyaltyStartUtc)
            .GroupBy(b => new
            {
                b.Service.BusinessId, b.Service.Business.Name, b.Service.Business.Slug, b.Service.Business.LoyaltyVisits,
                b.Service.Business.LoyaltyReward, b.Service.Business.LoyaltyStartUtc
            })
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var given = await dbContext.LoyaltyRedemptions.AsNoTracking()
            .Where(r => r.CustomerUserId == userId && r.RedeemedUtc >= r.Business.LoyaltyStartUtc)
            .GroupBy(r => r.BusinessId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken);

        return visits
            .Select(v =>
            {
                var needed = v.Key.LoyaltyVisits!.Value;
                var rewards = given.GetValueOrDefault(v.Key.BusinessId);
                var left = v.Count - rewards * needed;
                return new LoyaltyCardDto(v.Key.BusinessId, v.Key.Name, v.Key.Slug, needed, v.Key.LoyaltyReward ?? "",
                    Math.Clamp(left, 0, needed), left >= needed, rewards);
            })
            .OrderByDescending(c => c.RewardReady)
            .ThenByDescending(c => c.Stamps)
            .ToList();
    }

    // Visits that took place per customer account since the card started, with the name from the newest visit
    private static async Task<List<(string UserId, string Name, int Count, DateTime LastVisitUtc)>> VisitsAsync(
        IQueryable<Booking> bookings, DateTime startUtc, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var rows = await bookings.AsNoTracking()
            .Where(b => b.CustomerUserId != null && b.Status == BookingStatus.Bekreftet && b.StartUtc >= startUtc
                        && b.EndUtc <= nowUtc)
            .Select(b => new { b.CustomerUserId, b.CustomerName, b.StartUtc })
            .ToListAsync(cancellationToken);
        return rows.GroupBy(b => b.CustomerUserId!)
            .Select(g =>
            {
                var last = g.MaxBy(b => b.StartUtc)!;
                return (g.Key, last.CustomerName, g.Count(), last.StartUtc);
            })
            .ToList();
    }

    private static async Task<Dictionary<(Guid, string), int>> GivenAsync(IQueryable<LoyaltyRedemption> redemptions,
        DateTime startUtc, CancellationToken cancellationToken) =>
        (await redemptions.AsNoTracking()
            .Where(r => r.RedeemedUtc >= startUtc)
            .GroupBy(r => new { r.BusinessId, r.CustomerUserId })
            .Select(g => new { g.Key.BusinessId, g.Key.CustomerUserId, Count = g.Count() })
            .ToListAsync(cancellationToken))
        .ToDictionary(g => (g.BusinessId, g.CustomerUserId), g => g.Count);
}
