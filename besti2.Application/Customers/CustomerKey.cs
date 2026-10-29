namespace besti2.Application.Customers;

// The address of one customer in "Kunder": the account when there is one, otherwise the phone number
public static class CustomerKey
{
    public static string? For(string? customerUserId, string phone)
    {
        if (customerUserId is not null)
        {
            return $"k-{customerUserId}";
        }

        // Deleted and anonymized customers have "-" instead of a phone number: no card
        var digits = Digits(phone);
        return digits.Length >= 8 ? $"t-{digits}" : null;
    }

    // (account id, null) or (null, phone digits); null = not a key
    public static (string? UserId, string? Phone)? Parse(string? key)
    {
        if (key is null || key.Length < 3 || key.Length > 500)
        {
            return null;
        }

        var value = key[2..];
        return key[..2] switch
        {
            "k-" => (value, null),
            "t-" when value.All(char.IsDigit) && value.Length is >= 8 and <= 15 => (null, value),
            _ => null
        };
    }

    // "+47 912 34 567" and "91234567" are the same customer: compared on the last 8 digits
    public static string Digits(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return digits.Length > 8 && digits.StartsWith("47") ? digits[2..] : digits;
    }
}
