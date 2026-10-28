namespace besti2.Application.Businesses;

// "Verifisert bedrift": asks Brønnøysund again whether approved businesses are still active
public interface IBusinessVerifier
{
    // Checks the businesses not checked in the last week (oldest first). A business that is deleted,
    // bankrupt or being wound up loses the badge and BESTI gets a mail. Returns how many lost it
    Task<int> RecheckAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
}
