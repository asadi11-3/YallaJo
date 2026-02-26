using Finance.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Finance.Domain.Entities;

public sealed class DisputeEvidence : BaseEntity
{
    private DisputeEvidence() { } // EF Core

    public Guid DisputeId { get; private set; }
    public Guid UploadedByUserId { get; private set; }
    public string FileUrl { get; private set; } = string.Empty;
    public string? OriginalFileName { get; private set; }
    public string? Description { get; private set; }
    public EvidenceType EvidenceType { get; private set; }

    public Dispute Dispute { get; private set; } = default!;
}
