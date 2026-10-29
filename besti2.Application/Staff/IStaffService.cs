namespace besti2.Application.Staff;

public interface IStaffService
{
    // Owner: send the invitation e-mail (a new one replaces the old link). null = sent, otherwise the error
    Task<string?> InviteAsync(Guid businessId, Guid employeeId, string? email,
        CancellationToken cancellationToken = default);

    // Owner: take the access away again (or withdraw the invitation)
    Task<bool> RemoveAsync(Guid businessId, Guid employeeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StaffAccessDto>> GetAccessAsync(Guid businessId, CancellationToken cancellationToken = default);

    // The link from the e-mail; null = unknown or expired
    Task<StaffInviteInfoDto?> GetInviteAsync(string token, DateTime nowUtc,
        CancellationToken cancellationToken = default);

    // The logged-in employee accepts (only with the invited e-mail). null = connected, otherwise the error
    Task<string?> AcceptAsync(string userId, string token, DateTime nowUtc,
        CancellationToken cancellationToken = default);

    // Is this account an employee anywhere (for the "Min arbeidsplan" link)
    Task<bool> IsStaffAsync(string userId, CancellationToken cancellationToken = default);

    // The employee's own visits and blocked time from..from+days (days in Oslo)
    Task<IReadOnlyList<StaffPlaceDto>> GetScheduleAsync(string userId, DateOnly from, int days,
        CancellationToken cancellationToken = default);

    // The employee leaves on their own ("Fjern tilgang")
    Task<bool> LeaveAsync(string userId, Guid employeeId, CancellationToken cancellationToken = default);
}
