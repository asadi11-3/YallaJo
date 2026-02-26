using Messaging.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Messaging.Domain.Entities;

public sealed class SupportTicket : AuditableEntity, IAggregateRoot
{
    private readonly List<TicketMessage> _ticketMessages = [];

    private SupportTicket() { } // EF Core

    public Guid UserId { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public TicketStatus Status { get; private set; } = TicketStatus.Open;
    public TicketPriority Priority { get; private set; } = TicketPriority.Medium;
    public Guid? AssignedToUserId { get; private set; }
    public string? Category { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    public IReadOnlyCollection<TicketMessage> TicketMessages => _ticketMessages.AsReadOnly();
}
