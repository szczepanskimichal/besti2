using besti2.Application.Businesses;

namespace besti2.Tests;

// Brønnøysund without the internet: answers what the test sets
public class FakeBusinessRegistry : IBusinessRegistry
{
    public RegistryLookup Result { get; set; } =
        new(RegistryStatus.Active, "TEST FRISØR AS", "Testveien 1", "0150", "Oslo");

    public Task<RegistryLookup> LookupAsync(string orgNumber, CancellationToken cancellationToken = default)
        => Task.FromResult(Result);
}