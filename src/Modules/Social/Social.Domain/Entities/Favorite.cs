using Social.Domain.Enums;
using Social.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;

namespace Social.Domain.Entities;

/// <summary>
/// A user's saved favorite entity (tour, place, or business).
/// Max 500 per user (S-R7). Unique constraint on (UserId, EntityType, EntityId) where not soft-deleted.
/// </summary>
public sealed class Favorite : AuditableEntity, IAggregateRoot
{
    private Favorite() { } // EF Core

    public Guid UserId                  { get; private set; }
    public FavoriteEntityType EntityType { get; private set; }
    public Guid EntityId                { get; private set; }
    public DateTime AddedAt             { get; private set; }

    // ── Factory ──────────────────────────────────────────────────────────────

    public static Favorite Add(
        Guid userId,
        FavoriteEntityType entityType,
        Guid entityId,
        TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var fav = new Favorite
        {
            UserId     = userId,
            EntityType = entityType,
            EntityId   = entityId,
            AddedAt    = now,
        };
        fav.AddDomainEvent(new FavoriteAddedDomainEvent(fav.Id, userId, entityType, entityId, now));
        return fav;
    }

    // ── State transition ──────────────────────────────────────────────────────

    public void Remove(TimeProvider timeProvider)
    {
        if (IsDeleted) return; // idempotent
        var now = timeProvider.GetUtcNow().UtcDateTime;
        SoftDelete();
        AddDomainEvent(new FavoriteRemovedDomainEvent(Id, UserId, EntityType, EntityId, now));
    }
}
