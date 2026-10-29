namespace besti2.Application.Bookings;

// "Tid for ny time?": a reminder N weeks after each visit with this service, only when the customer asks for it
public interface IRebookReminderService
{
    // Weeks = 0 turns it off; returns an error text or null
    Task<string?> SetAsync(string userId, SetRebookReminderRequest request, DateTime nowUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RebookReminderDto>> GetMineAsync(string userId, CancellationToken cancellationToken = default);

    Task<bool> RemoveAsync(string userId, Guid id, CancellationToken cancellationToken = default);

    // The job: reminds those whose last visit was N weeks ago and who have not booked again. Returns how many
    Task<int> SendDueAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
}

public record SetRebookReminderRequest(Guid ServiceId, Guid? EmployeeId, int Weeks);

public record RebookReminderDto(Guid Id, string BusinessName, string BusinessSlug, Guid ServiceId, string ServiceName,
    Guid? EmployeeId, int Weeks);
