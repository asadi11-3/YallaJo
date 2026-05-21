using YallaJo.SharedKernel.Domain.Entities;

namespace Messaging.Domain.Entities;

public sealed class TicketMessage : BaseEntity
{
    private TicketMessage() { } // EF Core

    internal TicketMessage(Guid ticketId, Guid authorUserId, string body, bool isInternal)
    {
        TicketId     = ticketId;
        AuthorUserId = authorUserId;
        Body         = body;
        IsInternal   = isInternal;
        CreatedAt    = DateTime.UtcNow;
    }

    public Guid TicketId { get; private set; }

    /// <summary>UserId of the author (user or staff member).</summary>
    public Guid AuthorUserId { get; private set; }

    /// <summary>Message body.</summary>
    public string Body { get; private set; } = string.Empty;

    /// <summary>Internal staff-only note (hidden from user).</summary>
    public bool IsInternal { get; private set; }

    /// <summary>UTC timestamp of message creation.</summary>
    public DateTime CreatedAt { get; private set; }

    public SupportTicket SupportTicket { get; private set; } = default!;
}
