using ContentPlaces.Application.Queries.AccessibilityFeature.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Queries.AccessibilityFeature.GetAccessibilityFeatureCatalog;

/// <summary>
/// Public catalog of accessibility feature types — feeds admin pickers and client filter chips.
/// No filters, no pagination; the catalog is small and static.
/// </summary>
public sealed record GetAccessibilityFeatureCatalogQuery : IQuery<IReadOnlyList<AccessibilityFeatureCatalogItem>>;
