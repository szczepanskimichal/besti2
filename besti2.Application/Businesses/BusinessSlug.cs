using System.Globalization;
using System.Text;

namespace besti2.Application.Businesses;

// The short address of a business page, e.g. bestiapp.no/klipp-og-stil-frisor-gjovik.
// Made once from the name and the city at sign-up and never changed, so printed QR codes keep working
public static class BusinessSlug
{
    public const int MaxLength = 80;

    // Top-level pages of the website: a business can never get one of these addresses
    private static readonly HashSet<string> Reserved =
    [
        "aerlig-booking", "api", "bedrift", "bekreft-epost", "glemt-passord", "godkjenn", "konto-slettet", "logg-inn", "min-besti",
        "ny-tid", "panel", "personvern", "registrer", "registrer-bedrift", "robots.txt", "sitemap.xml", "uploads",
        "varsler", "vilkar", "favicon.ico", "_next", "health", "om", "hjelp", "kontakt", "admin"
    ];

    // "Klipp & Stil Frisør", "Gjøvik" -> "klipp-og-stil-frisor-gjovik"
    public static string From(string name, string city)
    {
        var text = $"{name} {city}".ToLowerInvariant()
            .Replace("&", " og ")
            .Replace("æ", "ae")
            .Replace("ø", "o")
            .Replace("å", "a");
        // é -> e, ü -> u ...: split letters from their accents and keep only the letter
        var builder = new StringBuilder();
        foreach (var c in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(c is >= 'a' and <= 'z' or >= '0' and <= '9' ? c : '-');
        }

        var slug = string.Join('-', builder.ToString().Split('-', StringSplitOptions.RemoveEmptyEntries));
        if (slug.Length > MaxLength)
        {
            slug = slug[..MaxLength].TrimEnd('-');
        }

        return slug.Length == 0 ? "bedrift" : slug;
    }

    public static bool IsReserved(string slug) => Reserved.Contains(slug);

    // Only what From can make: a-z, 0-9 and single dashes
    public static bool IsValid(string? slug) =>
        slug is { Length: > 0 and <= MaxLength }
        && slug.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-')
        && !slug.StartsWith('-') && !slug.EndsWith('-') && !slug.Contains("--");
}
