using Finance.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Finance.Domain.Entities;

public sealed class Dispute : AuditableEntity
{
    private readonly List<DisputeMessage> _messages = [];
    private readonly List<DisputeEvidence> _evidence = [];

    private Dispute() { } // EF Core

    public Guid PaymentId { get; private set; }
    public Guid UserId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DisputeStatus Status { get; private set; } = DisputeStatus.Open;
    public DisputeResolution? Resolution { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public Guid? ResolvedByUserId { get; private set; }
    public string? ResolutionNotes { get; private set; }

    public Payment Payment { get; private set; } = default!;
    public IReadOnlyCollection<DisputeMessage> Messages => _messages.AsReadOnly();
    public IReadOnlyCollection<DisputeEvidence> Evidence => _evidence.AsReadOnly();
}
