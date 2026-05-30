using Analytics.Domain.Enums;

namespace Analytics.Domain.ValueObjects;

public sealed record EntityRef(EntityType EntityType, Guid EntityId)
{
    public static EntityRef Tour(Guid id) => Create(EntityType.Tour, id);

    public static EntityRef Place(Guid id) => Create(EntityType.Place, id);

    public static EntityRef Business(Guid id) => Create(EntityType.Business, id);

    public static EntityRef Category(Guid id) => Create(EntityType.Category, id);

    public static EntityRef Create(EntityType entityType, Guid entityId)
    {
        if (entityId == Guid.Empty)
            throw new ArgumentException("Entity id cannot be empty.", nameof(entityId));

        return new EntityRef(entityType, entityId);
    }
}
