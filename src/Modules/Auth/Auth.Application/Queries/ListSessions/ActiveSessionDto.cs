namespace Auth.Application.Queries.ListSessions;

public sealed record ActiveSessionDto(
    Guid SessionId,
    Guid DeviceId,
    string? DeviceName,
    string? UserAgent,
    string? IpAddress,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    bool IsCurrent,
    bool IsTrusted);
