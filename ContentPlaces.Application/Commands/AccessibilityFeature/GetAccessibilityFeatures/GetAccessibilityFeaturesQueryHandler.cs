using ContentPlaces.Application.Queries.AccessibilityFeature.Common;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Queries.AccessibilityFeature.GetAccessibilityFeatures;

public sealed class GetAccessibilityFeaturesQueryHandler(
    IAccessibilityFeatureRepository featureRepository,
    ILogger<GetAccessibilityFeaturesQueryHandler> logger)
    : IQueryHandler<GetAccessibilityFeaturesQuery, IReadOnlyList<AccessibilityFeatureDto>>
{
    public async Task<Result<IReadOnlyList<AccessibilityFeatureDto>>> Handle(
        GetAccessibilityFeaturesQuery request,
        CancellationToken cancellationToken)
    {
        var features = await featureRepository.SelectAsync(
            selector: x => AccessibilityFeatureDto.From(x),
            filter: x => x.EntityId == request.PlaceId && x.EntityType == 1,
            orderBy: q => q.OrderBy(x => x.FeatureType).ThenBy(x => x.Name),
            ct: cancellationToken);

        logger.LogInformation(
            "Fetched {Count} accessibility features for Place {PlaceId}",
            features.Count, request.PlaceId);

        return Result<IReadOnlyList<AccessibilityFeatureDto>>.Success(features);
    }
}
