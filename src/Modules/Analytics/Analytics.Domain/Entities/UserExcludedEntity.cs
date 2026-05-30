using Analytics.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Analytics.Domain.Entities;

public sealed class UserExcludedEntity : BaseEntity, IAggregateRoot
{
    private UserExcludedEntity() { } // EF Core

    public Guid UserId { get; private set; }
    public EntityType EntityKind { get; private set; }
    public Guid EntityId { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    public static UserExcludedEntity Create(Guid userId, EntityType entityKind, Guid entityId, DateTime expiresAt)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        if (entityId == Guid.Empty)
            throw new ArgumentException("Entity id cannot be empty.", nameof(entityId));

        return new UserExcludedEntity
        {
            UserId = userId,
            EntityKind = entityKind,
            EntityId = entityId,
            ExpiresAt = expiresAt
        };
    }

    public bool IsExpired(DateTime now) => ExpiresAt <= now;
}
