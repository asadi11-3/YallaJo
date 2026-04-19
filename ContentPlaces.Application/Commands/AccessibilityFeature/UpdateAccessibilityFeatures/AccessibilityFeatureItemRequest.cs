using ContentPlaces.Domain.Enums;

namespace ContentPlaces.Application.Commands.AccessibilityFeature.UpdateAccessibilityFeatures;

public sealed record AccessibilityFeatureItemRequest(
    AccessibilityFeatureType FeatureType,
    string Name,
    string? Description,
    bool IsAvailable);
