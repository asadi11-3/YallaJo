using ContentCore.Domain.Enums;

namespace ContentCore.Domain.Entities;

public sealed class EntityCategory
{
    private EntityCategory() { } // EF Core

    public EntityType EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public Guid CategoryId { get; private set; }

    public Category Category { get; private set; } = default!;
}
