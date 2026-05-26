using ContentPlaces.Application.Queries.AccessibilityFeature.Common;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using AccessibilityFeatureEntity = ContentPlaces.Domain.Entities.AccessibilityFeature;

namespace ContentPlaces.Application.Queries.AccessibilityFeature.GetAccessibilityFeatures;

public sealed class GetAccessibilityFeaturesQueryHandler(
    IAccessibilityFeatureRepository featureRepository,
    IPlaceRepository placeRepository,
    ILogger<GetAccessibilityFeaturesQueryHandler> logger)
    : IQueryHandler<GetAccessibilityFeaturesQuery, IReadOnlyList<AccessibilityFeatureDto>>
{
    public async Task<Result<IReadOnlyList<AccessibilityFeatureDto>>> Handle(
        GetAccessibilityFeaturesQuery request,
        CancellationToken cancellationToken)
    {
        // Validate the parent Place is active (not missing, not soft-deleted).
        // IPlaceRepository.AnyAsync runs through the IsDeleted query filter on Place,
        // so soft-deleted/missing Places naturally return false. Without this guard,
        // accessibility features would remain visible after the parent Place is deleted
        // because AccessibilityFeature has no covering query filter (BaseEntity, no
        // soft-delete, keyed by composite EntityType+EntityId rather than a parent FK).
        var placeExists = await placeRepository.AnyAsync(
            p => p.Id == request.PlaceId,
            cancellationToken);

        if (!placeExists)
        {
            return Result<IReadOnlyList<AccessibilityFeatureDto>>.Failure(
                new Error("Place.NotFound", $"Place '{request.PlaceId}' was not found."),
                Outcome.NotFound);
        }

        var features = await featureRepository.SelectAsync(
            selector: x => AccessibilityFeatureDto.From(x),
            filter: x => x.EntityId == request.PlaceId && x.EntityType == AccessibilityFeatureEntity.EntityTypePlace,
            orderBy: q => q.OrderBy(x => x.FeatureType).ThenBy(x => x.Name),
            ct: cancellationToken);

        logger.LogInformation(
            "Fetched {Count} accessibility features for Place {PlaceId}",
            features.Count, request.PlaceId);

        return Result<IReadOnlyList<AccessibilityFeatureDto>>.Success(features);
    }
}
