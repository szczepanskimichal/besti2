using System.Net.Http.Json;
using System.Text.Json;
using besti2.Application.Geocoding;
using Microsoft.Extensions.Logging;

namespace besti2.Infrastructure.Services;

// Kartverket address API (free, no key): https://ws.geonorge.no/adresser/v1/
public class GeonorgeGeocodingService(HttpClient http, ILogger<GeonorgeGeocodingService> logger)
    : IGeocodingService
{
    public async Task<(double Latitude, double Longitude)?> GeocodeAsync(
        string address, string postalCode, CancellationToken cancellationToken = default)
    {
        var url =
            $"sok?sok={Uri.EscapeDataString(address)}&postnummer={Uri.EscapeDataString(postalCode)}&treffPerSide=1&utkoordsys=4258";
        try
        {
            var result = await http.GetFromJsonAsync<SearchResult>(url, cancellationToken);
            var point = result?.Adresser?.FirstOrDefault()?.Representasjonspunkt;
            return point is null ? null : (point.Lat, point.Lon);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            // Map is optional, so a failure must never block registration
            logger.LogWarning(ex, "Geocoding failed for {Address} {PostalCode}", address, postalCode);
            return null;
        }
    }

    private record SearchResult(List<AddressHit>? Adresser);

    private record AddressHit(Point? Representasjonspunkt);

    private record Point(double Lat, double Lon);
}