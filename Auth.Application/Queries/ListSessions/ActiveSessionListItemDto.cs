namespace Auth.Application.Queries.ListSessions;

/// <summary>
/// Stable session list item cached per user.
/// IMPORTANT: This type must NOT include request-scoped data (e.g., IsCurrent),
/// because the query result is cached by UserId.
/// </summary>
public sealed record ActiveSessionListItemDto(
    Guid SessionId,
    Guid DeviceId,
    string? DeviceName,
    string? UserAgent,
    string? IpAddress,
    DateTime CreatedAt,
    DateTime ExpiresAt);
