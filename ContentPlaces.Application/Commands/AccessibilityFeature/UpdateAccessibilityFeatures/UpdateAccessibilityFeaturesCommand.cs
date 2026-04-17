using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Commands.AccessibilityFeature.UpdateAccessibilityFeatures;

public sealed record UpdateAccessibilityFeaturesCommand(
    Guid PlaceId,
    IReadOnlyList<AccessibilityFeatureItemRequest> Features)
    : ICommand;
