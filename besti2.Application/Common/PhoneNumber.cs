namespace besti2.Application.Common;

public static class PhoneNumber
{
    // Digits, spaces and an optional + only, at least 8 digits (91234567, +47 912 34 567)
    public static bool IsValid(string? value)
    {
        var phone = value?.Trim() ?? "";
        return phone.Length is > 0 and <= 20
               && phone.All(c => char.IsDigit(c) || c == ' ' || c == '+')
               && phone.Count(char.IsDigit) >= 8
               && phone.LastIndexOf('+') <= 0;
    }
}