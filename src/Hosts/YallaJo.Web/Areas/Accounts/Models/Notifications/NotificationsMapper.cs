using YallaJo.Web.Features.Notifications;

namespace YallaJo.Web.Areas.Accounts.Models.Notifications;

/// <summary>
/// Static projections from the Messaging notification responses to the inbox VMs.
/// </summary>
public static class NotificationsMapper
{
    public static NotificationInboxRowVm ToRowVm(NotificationItemResponse n) => new(
        Id: n.Id,
        Type: n.Type,
        Title: n.Title,
        Body: n.Body,
        IsRead: n.IsRead,
        CreatedAt: n.CreatedAt,
        LinkUrl: NotificationLinkResolver.Resolve(n.EntityType, n.EntityId));

    public static NotificationsInboxVm ToInboxVm(
        NotificationPageResponse page,
        NotificationInboxFilterVm filter,
        int unreadCount) => new()
    {
        Filter      = filter,
        Items       = page.Items.Select(ToRowVm).ToList(),
        NextCursor  = page.NextCursor,
        UnreadCount = unreadCount,
    };
}
