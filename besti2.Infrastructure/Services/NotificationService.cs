using besti2.Application.Notifications;
using besti2.Domain.Entities;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services;

public class NotificationService(AppDbContext dbContext) : INotificationService
{
    private const int MaxShown = 30;

    public async Task<NotificationListDto> GetAsync(string userId, Guid? businessId,
        CancellationToken cancellationToken = default)
    {
        // Only what has not been read: a notification is gone once the list has been opened
        var mine = Mine(userId, businessId).Where(n => n.ReadUtc == null);
        var unread = await mine.CountAsync(cancellationToken);
        var items = await mine
            .OrderByDescending(n => n.CreatedUtc)
            .Take(MaxShown)
            .Select(n => new NotificationDto(n.Id, n.Text, n.Link, n.CreatedUtc, n.ReadUtc != null))
            .ToListAsync(cancellationToken);
        return new NotificationListDto(unread, items);
    }

    public async Task MarkAllReadAsync(string userId, Guid? businessId, CancellationToken cancellationToken = default)
    {
        // Read = cleared: the notifications are deleted (they are never shown again)
        var mine = await Mine(userId, businessId).ToListAsync(cancellationToken);
        dbContext.Notifications.RemoveRange(mine);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Notification> Mine(string userId, Guid? businessId) => businessId is Guid id
        ? dbContext.Notifications.Where(n => n.BusinessId == id)
        : dbContext.Notifications.Where(n => n.UserId == userId);
}