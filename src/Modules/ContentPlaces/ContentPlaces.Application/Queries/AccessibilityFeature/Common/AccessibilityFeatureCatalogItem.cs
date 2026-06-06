using ContentPlaces.Domain.Enums;

namespace ContentPlaces.Application.Queries.AccessibilityFeature.Common;

/// <summary>
/// Catalog item describing one accessibility-feature type that places/businesses can advertise.
/// Currently the catalog is the static <see cref="AccessibilityFeatureType"/> enum exposed
/// for client pickers; phase 3.5 may upgrade to an admin-mutable entity-backed catalog.
/// </summary>
public sealed record AccessibilityFeatureCatalogItem(
    AccessibilityFeatureType FeatureType,
    string Code,
    string DisplayName);
