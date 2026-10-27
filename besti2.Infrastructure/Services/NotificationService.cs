using besti2.Application.Notifications;
using besti2.Domain.Entities;
using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Services;

public class NotificationService(AppDbContext dbContext) : INotificationService
{
    private const int MaxShown = 30;

    // Kept until deleted, but never longer than this (so nothing hangs around when nobody deletes)
    public const int CustomerDays = 7;
    public const int BusinessDays = 14;

    public async Task<NotificationListDto> GetAsync(string userId, Guid? businessId,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-(businessId is null ? CustomerDays : BusinessDays));
        var expired = await Mine(userId, businessId).Where(n => n.CreatedUtc < cutoff).ToListAsync(cancellationToken);
        if (expired.Count > 0)
        {
            dbContext.Notifications.RemoveRange(expired);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var mine = Mine(userId, businessId);
        var unread = await mine.CountAsync(n => n.ReadUtc == null, cancellationToken);
        var items = await mine
            .OrderByDescending(n => n.CreatedUtc)
            .Take(MaxShown)
            .Select(n => new NotificationDto(n.Id, n.Text, n.Link, n.CreatedUtc, n.ReadUtc != null))
            .ToListAsync(cancellationToken);
        return new NotificationListDto(unread, items);
    }

    public async Task<NotificationReadDto?> MarkReadAsync(string userId, Guid? businessId, Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var notification = await Mine(userId, businessId)
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);
        if (notification is null)
        {
            return null;
        }

        notification.ReadUtc ??= DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new NotificationReadDto(notification.Link);
    }

    // Only the number on the bell (header and live stream): one COUNT, expired ones not counted
    public Task<int> UnreadCountAsync(string userId, Guid? businessId, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-(businessId is null ? CustomerDays : BusinessDays));
        return Mine(userId, businessId).CountAsync(n => n.ReadUtc == null && n.CreatedUtc >= cutoff, cancellationToken);
    }

    public async Task MarkAllReadAsync(string userId, Guid? businessId, CancellationToken cancellationToken = default)
    {
        var unread = await Mine(userId, businessId).Where(n => n.ReadUtc == null).ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var notification in unread)
        {
            notification.ReadUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> DeleteExpiredAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var customerCutoff = nowUtc.AddDays(-CustomerDays);
        var businessCutoff = nowUtc.AddDays(-BusinessDays);
        var expired = await dbContext.Notifications
            .Where(n => (n.BusinessId == null && n.CreatedUtc < customerCutoff)
                        || (n.BusinessId != null && n.CreatedUtc < businessCutoff))
            .ToListAsync(cancellationToken);
        dbContext.Notifications.RemoveRange(expired);
        await dbContext.SaveChangesAsync(cancellationToken);
        return expired.Count;
    }

    public async Task<bool> DeleteAsync(string userId, Guid? businessId, Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var notification = await Mine(userId, businessId)
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);
        if (notification is null)
        {
            return false;
        }

        dbContext.Notifications.Remove(notification);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IQueryable<Notification> Mine(string userId, Guid? businessId) => businessId is Guid id
        ? dbContext.Notifications.Where(n => n.BusinessId == id)
        : dbContext.Notifications.Where(n => n.UserId == userId);
}