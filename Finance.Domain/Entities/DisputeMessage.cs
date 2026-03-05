using YallaJo.SharedKernel.Domain.Entities;

namespace Finance.Domain.Entities;

public sealed class DisputeMessage : BaseEntity
{
    private DisputeMessage() { } // EF Core

    public Guid DisputeId { get; private set; }
    public Guid SenderUserId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public bool IsInternal { get; private set; }

    public Dispute Dispute { get; private set; } = default!;
}
