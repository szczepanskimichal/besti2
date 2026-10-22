using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using besti2.Application.Businesses;
using Microsoft.Extensions.Logging;

namespace besti2.Infrastructure.Services;

// Enhetsregisteret, Brønnøysundregistrene (free, no key): https://data.brreg.no/enhetsregisteret/api/
public class BrregBusinessRegistry(HttpClient http, ILogger<BrregBusinessRegistry> logger) : IBusinessRegistry
{
    public async Task<RegistryLookup> LookupAsync(string orgNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await http.GetAsync($"enheter/{orgNumber}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new RegistryLookup(RegistryStatus.NotFound);
            }
            // 410 Gone = deleted from the register
            if (response.StatusCode == HttpStatusCode.Gone)
            {
                return new RegistryLookup(RegistryStatus.Closed);
            }
            response.EnsureSuccessStatusCode();

            var unit = await response.Content.ReadFromJsonAsync<Unit>(cancellationToken);
            if (unit is null)
            {
                return new RegistryLookup(RegistryStatus.Unavailable);
            }
            if (unit.Slettedato is not null || unit.Konkurs || unit.UnderAvvikling
                || unit.UnderTvangsavviklingEllerTvangsopplosning)
            {
                return new RegistryLookup(RegistryStatus.Closed);
            }

            var address = unit.Forretningsadresse ?? unit.Postadresse;
            return new RegistryLookup(RegistryStatus.Active, unit.Navn,
                address?.Adresse is { Count: > 0 } lines ? string.Join(", ", lines) : null,
                address?.Postnummer, address?.Poststed is { } city ? ToTitle(city) : null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            // The register is down: registration must not stop because of someone else's problem
            logger.LogWarning(ex, "Brreg lookup failed for {OrgNumber}", orgNumber);
            return new RegistryLookup(RegistryStatus.Unavailable);
        }
    }

    // "OSLO" -> "Oslo", "MO I RANA" -> "Mo I Rana"
    private static string ToTitle(string text) =>
        System.Globalization.CultureInfo.GetCultureInfo("nb-NO").TextInfo.ToTitleCase(text.ToLowerInvariant());

    private record Unit(string? Navn, string? Slettedato, bool Konkurs, bool UnderAvvikling,
        bool UnderTvangsavviklingEllerTvangsopplosning, Address? Forretningsadresse, Address? Postadresse);

    private record Address(List<string>? Adresse, string? Postnummer, string? Poststed);
}
