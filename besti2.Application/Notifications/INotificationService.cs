namespace besti2.Application.Notifications;

public interface INotificationService
{
    // businessId set = the business's bell, otherwise the customer's (userId)
    Task<NotificationListDto> GetAsync(string userId, Guid? businessId, CancellationToken cancellationToken = default);
    Task MarkAllReadAsync(string userId, Guid? businessId, CancellationToken cancellationToken = default);
}