namespace YallaJo.Web.Areas.Admin.Models.Support;

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
