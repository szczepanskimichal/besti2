namespace besti2.Application.Accounts;

public record ConfirmEmailRequest(string UserId, string Code);