using System.Linq;

namespace YallaJo.Web.Areas.Admin.Models.Support;

public static class SupportMapper
{
    public static SupportListVm ToListVm(SupportTicketPageResponse page, string? statusFilter, string? categoryFilter)
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

    public static SupportTicketDetailVm ToDetailVm(SupportTicketItemResponse ticket)
    {
        return new SupportTicketDetailVm
        {
            Id = ticket.Id,
            CreatedByUserId = ticket.CreatedByUserId,
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
                Body = m.Body,
                IsInternal = m.IsInternal,
                CreatedAt = m.CreatedAt,
            }).ToList(),
        };
    }

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
}
