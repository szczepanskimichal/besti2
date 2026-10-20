namespace besti2.Application.Businesses;

public record FreeSlotDto(DateTime StartUtc, Guid ServiceId, string ServiceName, Guid EmployeeId);