using Social.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Social.Domain.Entities;

public sealed class Report : AuditableEntity, IAggregateRoot
{
    private Report() { } // EF Core

    public Guid ReporterUserId { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public ReportReason Reason { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public ReportStatus Status { get; private set; } = ReportStatus.Pending;
    public DateTime? ResolvedAt { get; private set; }
    public Guid? ResolvedByUserId { get; private set; }
    public string? ResolutionNotes { get; private set; }
}
