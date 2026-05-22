using Analytics.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Analytics.Domain.Entities;

public sealed class UserInteraction : BaseEntity<long>
{
    private UserInteraction() { }

    public Guid? UserId { get; private set; }
    public string? SessionId { get; private set; }
    public EntityType EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public InteractionType InteractionType { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public string? ClientIpHash { get; private set; }
    public string? UserAgent { get; private set; }

    public static UserInteraction Record(Guid? userId, string? sessionId, EntityType entityType, Guid entityId, InteractionType interactionType, DateTime occurredAt, string? clientIpHash, string? userAgent)
        => new() { UserId = userId, SessionId = sessionId, EntityType = entityType, EntityId = entityId, InteractionType = interactionType, OccurredAt = occurredAt, ClientIpHash = clientIpHash, UserAgent = userAgent };
}
