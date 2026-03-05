using ContentPlaces.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentPlaces.Domain.Entities;

public sealed class AccessibilityFeature : BaseEntity
{
    private AccessibilityFeature() { } // EF Core

    public byte EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public AccessibilityFeatureType FeatureType { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsAvailable { get; private set; }
}
