namespace Messaging.Application.Queries.Dtos;

public sealed record TicketMessageDto(
    Guid Id,
    Guid AuthorUserId,
    string Body,
    bool IsInternal,
    DateTime CreatedAt);

public sealed record SupportTicketDto(
    Guid Id,
    Guid CreatedByUserId,
    string Category,
    string Subject,
    string Priority,
    string Status,
    DateTime SlaBreachAt,
    Guid? AssignedToUserId,
    DateTime? AssignedAt,
    Guid? ResolvedByUserId,
    DateTime? ResolvedAt,
    string? ResolutionNotes,
    DateTime? ClosedAt,
    DateTime CreatedAt,
    string RowVersion,
    IReadOnlyList<TicketMessageDto>? Messages = null);

public sealed record SupportTicketPageDto(
    IReadOnlyList<SupportTicketDto> Items,
    Guid? NextCursor);
