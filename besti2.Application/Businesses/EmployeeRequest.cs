namespace besti2.Application.Businesses;

// Used both for adding and editing an employee
public record EmployeeRequest(string Name, string LastName);