using ContentPlaces.Application.Interfaces;
using ContentPlaces.Application.Queries.AccessibilityFeature.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Queries.AccessibilityFeature.GetAccessibilityFeatures;

public sealed class GetAccessibilityFeaturesQueryHandler(
    IContentPlacesDbContext dbContext,
    ILogger<GetAccessibilityFeaturesQueryHandler> logger)
    : IQueryHandler<GetAccessibilityFeaturesQuery, IReadOnlyList<AccessibilityFeatureDto>>
{
    public async Task<Result<IReadOnlyList<AccessibilityFeatureDto>>> Handle(
        GetAccessibilityFeaturesQuery request,
        CancellationToken cancellationToken)
    {
        var features = await dbContext.AccessibilityFeatures
            .AsNoTracking()
            .Where(x => x.EntityId == request.PlaceId && x.EntityType == 1)
            .OrderBy(x => x.FeatureType)
            .ThenBy(x => x.Name)
            .Select(x => AccessibilityFeatureDto.From(x))
            .ToListAsync(cancellationToken);

        logger.LogInformation(
            "Fetched {Count} accessibility features for Place {PlaceId}",
            features.Count,
            request.PlaceId);

        return Result<IReadOnlyList<AccessibilityFeatureDto>>.Success(features);
    }
}
