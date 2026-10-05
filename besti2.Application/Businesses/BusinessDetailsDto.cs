namespace besti2.Application.Businesses;

public record BusinessDetailsDto(
    Guid Id,
    string Name,
    string Category,
    string? Description,
    string Email,
    string PhoneNumber,
    string Address,
    string PostalCode,
    string City,
    IReadOnlyList<ServiceDto> Services,
    IReadOnlyList<EmployeeDto> Employees
    );