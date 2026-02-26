using Social.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Social.Domain.Entities;

public sealed class ContentModerationLog : BaseEntity
{
    private ContentModerationLog() { } // EF Core

    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public Guid ModeratorUserId { get; private set; }
    public ModerationAction Action { get; private set; }
    public string? Reason { get; private set; }
    public DateTime OccurredAt { get; private set; }
}
