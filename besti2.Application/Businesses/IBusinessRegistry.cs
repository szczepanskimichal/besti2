namespace besti2.Application.Businesses;

// The Norwegian business register (Enhetsregisteret in Brønnøysund)
public interface IBusinessRegistry
{
    Task<RegistryLookup> LookupAsync(string orgNumber, CancellationToken cancellationToken = default);
}

public enum RegistryStatus
{
    Active,
    NotFound,
    Closed, // deleted, bankrupt or being wound up
    Unavailable // the register did not answer
}

// Name and business address from the register (only when Active)
public record RegistryLookup(
    RegistryStatus Status,
    string? Name = null,
    string? Address = null,
    string? PostalCode = null,
    string? City = null);

public static class OrgNumber
{
    private static readonly int[] Weights = [3, 2, 7, 6, 5, 4, 3, 2];

    // 9 digits and the right check digit (modulus 11), so a typo is caught without asking the register
    public static bool IsValid(string? orgNumber)
    {
        if (orgNumber is not { Length: 9 } || !orgNumber.All(char.IsDigit))
        {
            return false;
        }

        var sum = Weights.Select((weight, i) => weight * (orgNumber[i] - '0')).Sum();
        var check = 11 - sum % 11;
        if (check == 11)
        {
            check = 0;
        }

        return check != 10 && check == orgNumber[8] - '0';
    }
}