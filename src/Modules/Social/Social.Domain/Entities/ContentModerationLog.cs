using Social.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Social.Domain.Entities;

/// <summary>
/// Append-only audit trail of admin moderation actions.
/// Never soft-deleted — serves as immutable record (S-R9 style for Social).
/// </summary>
public sealed class ContentModerationLog : BaseEntity
{
    private ContentModerationLog() { } // EF Core

    /// <summary>Creates a new append-only moderation log entry.</summary>
    public static ContentModerationLog Create(
        Guid adminUserId,
        ReportableEntityType entityType,
        Guid entityId,
        ModerationAction action,
        string? notes,
        DateTime actionedAt,
        Guid? sourceReportId = null) =>
        new(adminUserId, entityType, entityId, action, notes, actionedAt, sourceReportId);

    internal ContentModerationLog(
        Guid adminUserId,
        ReportableEntityType entityType,
        Guid entityId,
        ModerationAction action,
        string? notes,
        DateTime actionedAt,
        Guid? sourceReportId = null)
    {
        AdminUserId    = adminUserId;
        EntityType     = entityType;
        EntityId       = entityId;
        Action         = action;
        Notes          = notes;
        ActionedAt     = actionedAt;
        SourceReportId = sourceReportId;
    }

    public Guid AdminUserId             { get; private set; }
    public ReportableEntityType EntityType { get; private set; }
    public Guid EntityId                { get; private set; }
    public ModerationAction Action      { get; private set; }
    public string? Notes                { get; private set; }
    public DateTime ActionedAt          { get; private set; }
    /// <summary>The report that triggered this moderation action, if any.</summary>
    public Guid? SourceReportId         { get; private set; }
}
