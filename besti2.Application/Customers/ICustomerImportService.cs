namespace besti2.Application.Customers;

public interface ICustomerImportService
{
    // (result, null) or (null, the error for the whole file)
    Task<(CustomerImportResultDto? Result, string? Error)> ImportAsync(Guid businessId, CustomerImportRequest request,
        CancellationToken cancellationToken = default);

    // "Fjern fra Kunder": an imported customer who has never booked here, with the note; false = not found
    Task<bool> RemoveAsync(Guid businessId, string key, CancellationToken cancellationToken = default);
}
