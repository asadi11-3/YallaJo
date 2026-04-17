using ContentPlaces.Application.Queries.AccessibilityFeature.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Queries.AccessibilityFeature.GetAccessibilityFeatures;

public sealed record GetAccessibilityFeaturesQuery(Guid PlaceId)
    : IQuery<IReadOnlyList<AccessibilityFeatureDto>>;
