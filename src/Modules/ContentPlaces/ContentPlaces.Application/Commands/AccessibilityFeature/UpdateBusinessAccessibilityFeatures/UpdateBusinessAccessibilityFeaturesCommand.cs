using ContentPlaces.Application.Commands.AccessibilityFeature.UpdateAccessibilityFeatures;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Commands.AccessibilityFeature.UpdateBusinessAccessibilityFeatures;

public sealed record UpdateBusinessAccessibilityFeaturesCommand(
    Guid BusinessId,
    IReadOnlyList<AccessibilityFeatureItemRequest> Features)
    : ICommand;
