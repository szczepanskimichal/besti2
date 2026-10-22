namespace besti2.Application.Notifications;

public record NotificationDto(Guid Id, string Text, string? Link, DateTime CreatedUtc, bool IsRead);

// The newest notifications and how many are not read yet (the number on the bell)
public record NotificationListDto(int Unread, IReadOnlyList<NotificationDto> Items);

// After a click: where the notification leads
public record NotificationReadDto(string? Link);