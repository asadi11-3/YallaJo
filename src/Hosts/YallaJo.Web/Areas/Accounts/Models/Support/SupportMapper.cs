namespace YallaJo.Web.Areas.Accounts.Models.Support;

/// <summary>
/// FE-1C — static projections from the Messaging support-ticket API responses to the
/// user-facing Support view models.
/// <para>
/// Two invariants enforced here:
/// <list type="bullet">
///   <item>Internal staff notes (<c>IsInternal == true</c>) are NEVER projected into the
///   thread the owner sees.</item>
///   <item>A ticket is actionable (reply / close) only while it is not terminal
///   (Closed/Resolved). The backend is the authority; this only governs UI affordances.</item>
/// </list>
/// </para>
/// </summary>
public static class SupportMapper
{
    // Terminal statuses where the owner can no longer reply or close.
    private static bool IsTerminal(string status) =>
        string.Equals(status, "Closed", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, "Resolved", StringComparison.OrdinalIgnoreCase);

    private static bool IsClosedStatus(string status) =>
        string.Equals(status, "Closed", StringComparison.OrdinalIgnoreCase);

    public static SupportTicketRowVm ToRowVm(SupportTicketItemResponse t) => new(
        Id: t.Id,
        Category: t.Category,
        Subject: t.Subject,
        Status: t.Status,
        CreatedAt: t.CreatedAt,
        IsClosed: IsClosedStatus(t.Status));

    public static SupportListVm ToListVm(SupportTicketPageResponse page, string? loadError = null) => new()
    {
        Items = page.Items.Select(ToRowVm).ToList(),
        NextCursor = page.NextCursor,
        LoadError = loadError,
    };

    public static SupportListVm DegradedListVm(string loadError) => new()
    {
        Items = [],
        NextCursor = null,
        LoadError = loadError,
    };

    public static SupportTicketDetailVm ToDetailVm(SupportTicketItemResponse t)
    {
        var messages = (t.Messages ?? [])
            // Hide internal staff notes from the ticket owner.
            .Where(m => !m.IsInternal)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new SupportMessageVm(
                Body: m.Body,
                // A message authored by someone other than the ticket creator is staff.
                IsFromStaff: m.AuthorUserId != t.CreatedByUserId,
                CreatedAt: m.CreatedAt))
            .ToList();

        var actionable = !IsTerminal(t.Status);

        return new SupportTicketDetailVm
        {
            Id = t.Id,
            Subject = t.Subject,
            Category = t.Category,
            Status = t.Status,
            CreatedAt = t.CreatedAt,
            ClosedAt = t.ClosedAt,
            RowVersion = t.RowVersion ?? string.Empty,
            Messages = messages,
            CanReply = actionable,
            CanClose = actionable,
        };
    }
}
