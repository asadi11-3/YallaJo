using YallaJo.SharedKernel.Domain.Entities;

namespace Messaging.Domain.Entities;

public sealed class TicketMessage : BaseEntity
{
    private TicketMessage() { } // EF Core

    public Guid TicketId { get; private set; }
    public Guid SenderUserId { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public bool IsStaffReply { get; private set; }

    public SupportTicket SupportTicket { get; private set; } = default!;
}
