namespace besti2.Application.Accounts;

public interface ICustomerAccountService
{
    Task<string?> RegisterAsync(RegisterCustomerRequest request, CancellationToken cancellationToken = default);

    // Confirms the email with the code from the mail; returns false when the link is wrong or expired
    Task<bool> ConfirmEmailAsync(ConfirmEmailRequest request);
}