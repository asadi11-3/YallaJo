using ContentBlogs.Domain.Enums;

namespace ContentBlogs.Domain.ValueObjects;

/// <summary>
/// Declares a creator's relationship to a tagged entity for transparency.
/// Stored as an owned collection on <see cref="Entities.Creators.CreatorPost"/>.
/// Wave 8 – Disclosure enforcement.
/// </summary>
public sealed record DisclosureTarget
{
    /// <summary>The kind of entity being disclosed (Place, Tour, Business, etc.).</summary>
    public string EntityType { get; init; } = default!;

    /// <summary>Primary key of the disclosed entity.</summary>
    public Guid EntityId { get; init; }

    /// <summary>Nature of the relationship.</summary>
    public DisclosureRelationKind RelationKind { get; init; }

    /// <summary>EF Core requires a parameterless constructor.</summary>
    private DisclosureTarget() { }

    public DisclosureTarget(string entityType, Guid entityId, DisclosureRelationKind relationKind)
    {
        EntityType = entityType;
        EntityId = entityId;
        RelationKind = relationKind;
    }
}
