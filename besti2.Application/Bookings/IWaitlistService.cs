namespace besti2.Application.Bookings;

// "Si fra hvis det blir ledig" (venteliste): free for customers, the business gets cancelled times filled again
public interface IWaitlistService
{
    // Returns an error text or null
    Task<string?> JoinAsync(string userId, JoinWaitlistRequest request, DateTime nowUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WaitlistEntryDto>> GetMineAsync(string userId, DateTime nowUtc,
        CancellationToken cancellationToken = default);

    Task<bool> LeaveAsync(string userId, Guid entryId, CancellationToken cancellationToken = default);

    // The job: tells the waiting customers about new free times and removes days that have passed.
    // Returns how many customers got a message
    Task<int> CheckAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
}

public record JoinWaitlistRequest(Guid ServiceId, Guid? EmployeeId, DateOnly Date);

public record WaitlistEntryDto(Guid Id, string BusinessName, string BusinessSlug, Guid ServiceId, string ServiceName,
    Guid? EmployeeId, string? EmployeeName, DateOnly Date, bool Notified);
