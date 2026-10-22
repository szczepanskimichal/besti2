namespace besti2.Application.Notifications;

public interface INotificationService
{
    // businessId set = the business's bell, otherwise the customer's (userId).
    // Old notifications are removed here: 7 days for a customer, 14 days for a business
    Task<NotificationListDto> GetAsync(string userId, Guid? businessId, CancellationToken cancellationToken = default);
    // One notification read (a click on it); returns where it leads, or null when it is not this user's
    Task<NotificationReadDto?> MarkReadAsync(string userId, Guid? businessId, Guid notificationId,
        CancellationToken cancellationToken = default);
    // Only the number on the bell (for the live stream)
    Task<int> UnreadCountAsync(string userId, Guid? businessId, CancellationToken cancellationToken = default);
    Task MarkAllReadAsync(string userId, Guid? businessId, CancellationToken cancellationToken = default);
    // Daily cleanup for everybody (also those who never open the bell): 7 days customer, 14 days business
    Task<int> DeleteExpiredAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
    // The bin (🗑️) next to one notification; false when it is not this user's
    Task<bool> DeleteAsync(string userId, Guid? businessId, Guid notificationId,
        CancellationToken cancellationToken = default);
}