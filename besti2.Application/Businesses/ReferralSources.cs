namespace besti2.Application.Businesses;

// "Hvor hørte du om BESTI?" at business sign-up: the only answers BESTI accepts (the form shows the same list)
public static class ReferralSources
{
    public static readonly string[] All =
    [
        "Besøk fra BESTI",
        "E-post fra BESTI",
        "Instagram",
        "Facebook",
        "Google",
        "En annen bedrift",
        "En kunde",
        "Annet"
    ];

    // null when empty or not one of the answers (the question is optional)
    public static string? Clean(string? value) =>
        All.FirstOrDefault(s => s == value?.Trim());
}
