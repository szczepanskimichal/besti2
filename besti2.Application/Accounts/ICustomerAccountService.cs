namespace besti2.Application.Accounts;

public interface ICustomerAccountService
{
    Task<string?> RegisterAsync(RegisterCustomerRequest request, CancellationToken cancellationToken = default);

    // Confirms the email with the code from the mail; returns false when the link is wrong or expired
    Task<bool> ConfirmEmailAsync(ConfirmEmailRequest request);

    // /admin support: a new confirmation link to a customer who never got (or lost) the first one.
    // null = sent, otherwise why not
    Task<string?> ResendConfirmationAsync(string userId, CancellationToken cancellationToken = default);
}