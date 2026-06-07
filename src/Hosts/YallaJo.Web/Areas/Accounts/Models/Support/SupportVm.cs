namespace YallaJo.Web.Areas.Accounts.Models.Support;

/// <summary>One row in the "My Support Tickets" list (FE-1C).</summary>
public sealed record SupportTicketRowVm(
    Guid Id,
    string Category,
    string Subject,
    string Status,
    DateTime CreatedAt,
    bool IsClosed);

/// <summary>The My Support Tickets list page view model (cursor "load more").</summary>
public sealed class SupportListVm
{
    public IReadOnlyList<SupportTicketRowVm> Items { get; init; } = [];
    public Guid? NextCursor { get; init; }

    /// <summary>Non-null when the API call degraded; the view shows a banner but still renders.</summary>
    public string? LoadError { get; init; }

    public bool HasItems => Items.Count > 0;
    public bool HasNextPage => NextCursor is { } c && c != Guid.Empty;
}

/// <summary>One message in a ticket thread, projected for the owner's eyes.</summary>
public sealed record SupportMessageVm(
    string Body,
    bool IsFromStaff,
    DateTime CreatedAt);

/// <summary>The ticket detail / thread page view model.</summary>
public sealed class SupportTicketDetailVm
{
    public Guid Id { get; init; }
    public string Subject { get; init; } = "";
    public string Category { get; init; } = "";
    public string Status { get; init; } = "";
    public DateTime CreatedAt { get; init; }
    public DateTime? ClosedAt { get; init; }

    /// <summary>Optimistic-concurrency token (base64) carried back to the close POST.</summary>
    public string RowVersion { get; init; } = "";

    public IReadOnlyList<SupportMessageVm> Messages { get; init; } = [];

    /// <summary>
    /// True when the ticket is in a state the owner can still act on (not Closed/Resolved).
    /// Drives both the reply form and the close button. Whether the close button actually
    /// renders is additionally gated on the SupportTicket.Close permission in the view.
    /// </summary>
    public bool CanReply { get; init; }
    public bool CanClose { get; init; }
}
