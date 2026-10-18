namespace besti2.Application.Geocoding;

public interface IGeocodingService
{
    // Returns coordinates for a Norwegian address, or null when not found
    Task<(double Latitude, double Longitude)?> GeocodeAsync(
        string address, string postalCode, CancellationToken cancellationToken = default);
}