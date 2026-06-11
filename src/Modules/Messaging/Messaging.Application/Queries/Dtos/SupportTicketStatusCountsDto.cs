namespace Messaging.Application.Queries.Dtos;

/// <summary>
/// Per-status support ticket counts for the admin queue counted tabs.
/// Covers every <see cref="Messaging.Domain.Enums.TicketStatus"/> value.
/// </summary>
public sealed record SupportTicketStatusCountsDto(
    int Open,
    int Assigned,
    int InProgress,
    int AwaitingUser,
    int Resolved,
    int Closed);
