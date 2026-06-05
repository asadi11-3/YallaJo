namespace YallaJo.Web.Features.Notifications;

/// <summary>
/// View model backing the shared notification-bell dropdown rendered in the header.
/// </summary>
public sealed class NotificationBellVm
{
    public int UnreadCount { get; init; }
    public IReadOnlyList<NotificationRowVm> Recent { get; init; } = [];

    public bool HasUnread => UnreadCount > 0;
    public bool HasRecent => Recent.Count > 0;
}

public sealed record NotificationRowVm(
    Guid Id,
    string Title,
    string Body,
    bool IsRead,
    DateTime CreatedAt);
