using besti2.Application.Geocoding;

namespace besti2.Tests;

public class FakeGeocodingService : IGeocodingService
{
    public (double Latitude, double Longitude)? Result { get; set; } = (59.91, 10.75);

    public Task<(double Latitude, double Longitude)?> GeocodeAsync(
        string address, string postalCode, CancellationToken cancellationToken = default)
        => Task.FromResult(Result);
}