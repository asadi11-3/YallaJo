namespace YallaJo.Web.Features.Notifications;

/// <summary>
/// A single notification item returned by the Messaging API
/// (GET /api/v1/notifications). Type/Channel/Priority are serialized as strings.
/// </summary>
public sealed class NotificationItemResponse
{
    public Guid Id { get; init; }
    public string Type { get; init; } = "";
    public string Title { get; init; } = "";
    public string Body { get; init; } = "";
    public bool IsRead { get; init; }
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Cursor-paged notification list (GET /api/v1/notifications).
/// </summary>
public sealed class NotificationPageResponse
{
    public IReadOnlyList<NotificationItemResponse> Items { get; init; } = [];
    public Guid? NextCursor { get; init; }
    public int? TotalCount { get; init; }
}
