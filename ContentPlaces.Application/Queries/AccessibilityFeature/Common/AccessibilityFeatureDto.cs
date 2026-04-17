using ContentPlaces.Domain.Enums;
using AccessibilityFeatureEntity = ContentPlaces.Domain.Entities.AccessibilityFeature;

namespace ContentPlaces.Application.Queries.AccessibilityFeature.Common;

public sealed record AccessibilityFeatureDto(
    AccessibilityFeatureType FeatureType,
    string Name,
    string? Description,
    bool IsAvailable)
{
    public static AccessibilityFeatureDto From(AccessibilityFeatureEntity feature) => new(
        feature.FeatureType,
        feature.Name,
        feature.Description,
        feature.IsAvailable);
}
