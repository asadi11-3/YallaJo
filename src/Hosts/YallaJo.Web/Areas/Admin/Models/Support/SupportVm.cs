namespace YallaJo.Web.Areas.Admin.Models.Support;

public sealed class SupportListVm
{
    public IReadOnlyList<SupportTicketRowVm> Tickets { get; set; } = [];
    public Guid? NextCursor { get; set; }
    public string? StatusFilter { get; set; }
    public string? CategoryFilter { get; set; }

    /// <summary>
    /// Per-status queue counts for counted tabs (§5.6). Null when the counts
    /// call failed — tabs render without badges (ERR3 best-effort decoration).
    /// </summary>
    public SupportTicketStatusCountsVm? StatusCounts { get; set; }
}

public sealed class SupportTicketStatusCountsVm
{
    public int Open { get; init; }
    public int Assigned { get; init; }
    public int InProgress { get; init; }
    public int AwaitingUser { get; init; }
    public int Resolved { get; init; }
    public int Closed { get; init; }
}

public sealed class SupportTicketRowVm
{
    public Guid Id { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Category { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Priority { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime SlaBreachAt { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? RequesterEmail { get; set; }
    public string? AssigneeEmail { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class SupportTicketDetailVm
{
    public Guid Id { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string? RequesterEmail { get; set; }
    public string Category { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Priority { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime SlaBreachAt { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? AssigneeEmail { get; set; }
    public DateTime? AssignedAt { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public string? ResolverEmail { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string RowVersion { get; set; } = "";
    public IReadOnlyList<TicketMessageVm> Messages { get; set; } = [];
    public IReadOnlyList<SupportAdminOptionVm> AdminOptions { get; set; } = [];
    public bool IsClosed { get; set; }
    public bool IsResolved { get; set; }
}

public sealed class TicketMessageVm
{
    public Guid Id { get; set; }
    public Guid AuthorUserId { get; set; }
    public string? AuthorEmail { get; set; }
    public string Body { get; set; } = "";
    public bool IsInternal { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class SupportAdminOptionVm
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
}
