using Social.Domain.Enums;
using Social.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;

namespace Social.Domain.Entities;

/// <summary>An abuse / policy-violation report submitted by a user against any reportable entity.</summary>
public sealed class Report : AuditableEntity, IAggregateRoot
{
    private Report() { } // EF Core

    public Guid ReporterUserId              { get; private set; }
    public ReportableEntityType EntityType  { get; private set; }
    public Guid EntityId                    { get; private set; }
    public ReportReason Reason              { get; private set; }
    public string Description               { get; private set; } = string.Empty;
    public ReportStatus Status              { get; private set; } = ReportStatus.Open;
    public DateTime SubmittedAt             { get; private set; }
    public Guid? ResolvedByUserId           { get; private set; }
    public DateTime? ResolvedAt             { get; private set; }
    public ModerationAction? ResolutionAction { get; private set; }
    public string? ResolutionNotes          { get; private set; }

    // ── Factory ──────────────────────────────────────────────────────────────

    public static Report Submit(
        Guid reporterUserId,
        ReportableEntityType entityType,
        Guid entityId,
        ReportReason reason,
        string description,
        TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var report = new Report
        {
            ReporterUserId = reporterUserId,
            EntityType     = entityType,
            EntityId       = entityId,
            Reason         = reason,
            Description    = description,
            Status         = ReportStatus.Open,
            SubmittedAt    = now,
        };
        report.AddDomainEvent(new ReportSubmittedDomainEvent(
            report.Id, reporterUserId, entityType, entityId, reason, now));
        return report;
    }

    // ── State transition ──────────────────────────────────────────────────────

    public void MarkUnderReview()
    {
        if (Status != ReportStatus.Open) return;
        Status = ReportStatus.UnderReview;
        MarkUpdated();
    }

    public void Resolve(
        Guid adminUserId,
        ModerationAction action,
        string? notes,
        TimeProvider timeProvider)
    {
        if (Status == ReportStatus.Resolved || Status == ReportStatus.Dismissed)
            return; // idempotent

        var now    = timeProvider.GetUtcNow().UtcDateTime;
        Status     = action == ModerationAction.Dismiss ? ReportStatus.Dismissed : ReportStatus.Resolved;
        ResolvedByUserId  = adminUserId;
        ResolvedAt        = now;
        ResolutionAction  = action;
        ResolutionNotes   = notes;
        MarkUpdated();

        AddDomainEvent(new ReportResolvedDomainEvent(Id, adminUserId, action, now));
    }
}
