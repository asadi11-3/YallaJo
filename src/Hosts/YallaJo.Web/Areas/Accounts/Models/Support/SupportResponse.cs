namespace YallaJo.Web.Areas.Accounts.Models.Support;

/// <summary>
/// FE-1C — Accounts-local copies of the Messaging support-ticket API responses.
/// Intentionally duplicated from the Admin area's equivalents (per-area model
/// convention) so the user-facing Support pages do not couple to Admin code.
/// Shapes mirror Messaging.Application.Queries.Dtos.SupportTicketDto / *PageDto.
/// </summary>
public sealed class SupportTicketPageResponse
{
    public IReadOnlyList<SupportTicketItemResponse> Items { get; set; } = [];
    public Guid? NextCursor { get; set; }
}

public sealed class SupportTicketItemResponse
{
    public Guid Id { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Category { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Priority { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime SlaBreachAt { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public DateTime? AssignedAt { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string RowVersion { get; set; } = "";
    public IReadOnlyList<TicketMessageResponse>? Messages { get; set; }
}

public sealed class TicketMessageResponse
{
    public Guid Id { get; set; }
    public Guid AuthorUserId { get; set; }
    public string Body { get; set; } = "";
    public bool IsInternal { get; set; }
    public DateTime CreatedAt { get; set; }
}
