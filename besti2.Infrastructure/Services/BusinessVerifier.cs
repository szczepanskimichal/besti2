using besti2.Application.Businesses;
using besti2.Application.Email;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace besti2.Infrastructure.Services;

// adminEmail = where BESTI hears about a business that is no longer active (no mail when null)
public class BusinessVerifier(
    AppDbContext dbContext,
    IBusinessRegistry registry,
    IEmailService emailService,
    ILogger<BusinessVerifier> logger,
    string? adminEmail = null) : IBusinessVerifier
{
    // A badge is at most this old (plus one day, the job runs daily)
    public static readonly TimeSpan CheckEvery = TimeSpan.FromDays(7);

    // Asked per run; enough for 1 400 businesses a week without being rude to the free register
    public const int BatchSize = 200;

    public async Task<int> RecheckAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var due = nowUtc - CheckEvery;
        // Never checked first (null sorts first), then the oldest
        var businesses = await dbContext.Businesses
            .Where(b => b.ApprovedUtc != null && (b.VerifiedUtc == null || b.VerifiedUtc < due))
            .OrderBy(b => b.VerifiedUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        var lost = new List<string>();
        foreach (var business in businesses)
        {
            var lookup = await registry.LookupAsync(business.OrgNumber, cancellationToken);
            if (lookup.Status == RegistryStatus.Unavailable)
            {
                // The register is down: keep every badge as it is and try again tomorrow
                break;
            }

            if (lookup.Status == RegistryStatus.Active)
            {
                business.VerifiedUtc = nowUtc;
                continue;
            }

            // Only a business that had the badge is reported, so BESTI gets one mail, not one every day
            if (business.VerifiedUtc is not null)
            {
                lost.Add($"{business.Name} (org.nr. {business.OrgNumber}): " +
                         $"https://virksomhet.brreg.no/nb/oppslag/enheter/{business.OrgNumber}");
            }

            business.VerifiedUtc = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        if (lost.Count > 0)
        {
            logger.LogWarning("{Count} businesses are no longer active in Brønnøysund", lost.Count);
            await TryTellAdminAsync(lost, cancellationToken);
        }

        return lost.Count;
    }

    private async Task TryTellAdminAsync(List<string> lost, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(adminEmail))
        {
            return;
        }

        var body =
            "Disse bedriftene er ikke lenger aktive i Enhetsregisteret (slettet, konkurs eller under avvikling).\n" +
            "De har mistet merket «Verifisert bedrift», men er fortsatt synlige for kundene:\n\n" +
            string.Join("\n", lost) + "\n\n" +
            "Sjekk dem og kontakt bedriften om nødvendig.\n";
        try
        {
            await emailService.SendAsync(adminEmail, "Bedrifter som ikke lenger er aktive i Brønnøysund", body,
                cancellationToken);
        }
        catch (Exception ex)
        {
            // The badges are already removed; the log has the count
            logger.LogWarning(ex, "Could not tell BESTI about inactive businesses");
        }
    }
}
