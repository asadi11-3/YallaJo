using System.Linq;

namespace YallaJo.Web.Areas.Admin.Models.Support;

public static class SupportMapper
{
    public static SupportListVm ToListVm(SupportTicketPageResponse page, string? statusFilter, string? categoryFilter, IReadOnlyDictionary<Guid, string>? userEmails = null)
    {
        return new SupportListVm
        {
            NextCursor = page.NextCursor,
            StatusFilter = statusFilter,
            CategoryFilter = categoryFilter,
            Tickets = page.Items.Select(t => new SupportTicketRowVm
            {
                Id = t.Id,
                CreatedByUserId = t.CreatedByUserId,
                RequesterEmail = Lookup(userEmails, t.CreatedByUserId),
                AssigneeEmail = t.AssignedToUserId is { } aid ? Lookup(userEmails, aid) : null,
                Category = t.Category,
                Subject = t.Subject,
                Priority = t.Priority,
                Status = t.Status,
                SlaBreachAt = t.SlaBreachAt,
                AssignedToUserId = t.AssignedToUserId,
                CreatedAt = t.CreatedAt,
            }).ToList(),
        };
    }

    public static SupportTicketDetailVm ToDetailVm(
        SupportTicketItemResponse ticket,
        IReadOnlyDictionary<Guid, string>? userEmails = null,
        IReadOnlyList<SupportAdminOptionVm>? adminOptions = null)
    {
        return new SupportTicketDetailVm
        {
            Id = ticket.Id,
            CreatedByUserId = ticket.CreatedByUserId,
            RequesterEmail = Lookup(userEmails, ticket.CreatedByUserId),
            AssigneeEmail = ticket.AssignedToUserId is { } aid ? Lookup(userEmails, aid) : null,
            ResolverEmail = ticket.ResolvedByUserId is { } rid ? Lookup(userEmails, rid) : null,
            AdminOptions = adminOptions ?? [],
            Category = ticket.Category,
            Subject = ticket.Subject,
            Priority = ticket.Priority,
            Status = ticket.Status,
            SlaBreachAt = ticket.SlaBreachAt,
            AssignedToUserId = ticket.AssignedToUserId,
            AssignedAt = ticket.AssignedAt,
            ResolvedByUserId = ticket.ResolvedByUserId,
            ResolvedAt = ticket.ResolvedAt,
            ResolutionNotes = ticket.ResolutionNotes,
            ClosedAt = ticket.ClosedAt,
            CreatedAt = ticket.CreatedAt,
            RowVersion = ticket.RowVersion,
            IsClosed = string.Equals(ticket.Status, "Closed", StringComparison.OrdinalIgnoreCase),
            IsResolved = string.Equals(ticket.Status, "Resolved", StringComparison.OrdinalIgnoreCase),
            Messages = (ticket.Messages ?? []).Select(m => new TicketMessageVm
            {
                Id = m.Id,
                AuthorUserId = m.AuthorUserId,
                AuthorEmail = Lookup(userEmails, m.AuthorUserId),
                Body = m.Body,
                IsInternal = m.IsInternal,
                CreatedAt = m.CreatedAt,
            }).ToList(),
        };
    }

    public static SupportTicketStatusCountsVm ToStatusCountsVm(SupportTicketStatusCountsResponse r) => new()
    {
        Open = r.Open,
        Assigned = r.Assigned,
        InProgress = r.InProgress,
        AwaitingUser = r.AwaitingUser,
        Resolved = r.Resolved,
        Closed = r.Closed,
    };

    public static string StatusColor(string status) => status switch
    {
        "Open" => "warning",
        "Assigned" => "info",
        "InProgress" => "info",
        "AwaitingUser" => "secondary",
        "Resolved" => "success",
        "Closed" => "secondary",
        _ => "secondary",
    };

    public static string StatusIcon(string status) => status switch
    {
        "Open" => "clock",
        "Assigned" => "user-check",
        "InProgress" => "spinner",
        "AwaitingUser" => "hourglass-half",
        "Resolved" => "circle-check",
        "Closed" => "circle-xmark",
        _ => "circle-info",
    };

    private static string? Lookup(IReadOnlyDictionary<Guid, string>? emails, Guid id)
        => emails is not null && emails.TryGetValue(id, out var email) ? email : null;
}
