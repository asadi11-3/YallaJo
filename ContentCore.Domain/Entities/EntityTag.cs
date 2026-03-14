using ContentCore.Domain.Enums;

namespace ContentCore.Domain.Entities;

public sealed class EntityTag
{
    private EntityTag() { } // EF Core

    public EntityType EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public Guid TagId { get; private set; }

    public Tag Tag { get; private set; } = default!;

    // ── Factory Method ──
    public static EntityTag Create(EntityType entityType, Guid entityId, Guid tagId)
    {
        return new EntityTag
        {
            EntityType = entityType,
            EntityId = entityId,
            TagId = tagId
        };
    }
}
