using Messaging.Domain.Enums;

namespace Messaging.Application.Queries.Dtos;

/// <summary>Projection of a single notification for API responses.</summary>
public sealed record NotificationDto(
    Guid Id,
    Guid UserId,
    string Type,
    string Channel,
    string Priority,
    string Title,
    string Body,
    string? Data,
    bool IsRead,
    DateTime? ReadAt,
    DateTime? SentAt,
    string? EntityType,
    Guid? EntityId,
    DateTime CreatedAt);

/// <summary>Cursor-based page of notifications.</summary>
public sealed record NotificationPageDto(
    IReadOnlyList<NotificationDto> Items,
    Guid? NextCursor,
    int? TotalCount = null);
