using Social.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Social.Domain.Entities;

/// <summary>Admin warning / ban record used for Social moderation strike history.</summary>
public sealed class UserModerationRecord : AuditableEntity, IAggregateRoot
{
    private UserModerationRecord() { } // EF Core

    public Guid UserId { get; private set; }
    public ReportableEntityType EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public ModerationAction Action { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTime? ExpiresAt { get; private set; }
    public Guid IssuedByAdminId { get; private set; }
    public DateTime IssuedAt { get; private set; }

    public static UserModerationRecord Issue(
        Guid userId,
        ReportableEntityType entityType,
        Guid entityId,
        ModerationAction action,
        string reason,
        DateTime? expiresAt,
        Guid issuedByAdminId,
        TimeProvider timeProvider)
    {
        if (action is not (ModerationAction.WarnUser or ModerationAction.BanUser))
            throw new ArgumentOutOfRangeException(nameof(action), "Only warning and ban actions are moderation records.");

        return new UserModerationRecord
        {
            UserId = userId,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Reason = string.IsNullOrWhiteSpace(reason) ? "Moderation action" : reason.Trim(),
            ExpiresAt = expiresAt,
            IssuedByAdminId = issuedByAdminId,
            IssuedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
    }
}
