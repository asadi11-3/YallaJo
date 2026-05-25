using ContentPlaces.Application.Queries.AccessibilityFeature.Common;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Queries.AccessibilityFeature.GetBusinessAccessibilityFeatures;

public sealed class GetBusinessAccessibilityFeaturesQueryHandler(
    IAccessibilityFeatureRepository featureRepository,
    IBusinessRepository businessRepository,
    ILogger<GetBusinessAccessibilityFeaturesQueryHandler> logger)
    : IQueryHandler<GetBusinessAccessibilityFeaturesQuery, IReadOnlyList<AccessibilityFeatureDto>>
{
    private const byte BusinessEntityType = 2;

    public async Task<Result<IReadOnlyList<AccessibilityFeatureDto>>> Handle(
        GetBusinessAccessibilityFeaturesQuery request,
        CancellationToken cancellationToken)
    {
        var businessExists = await businessRepository.AnyAsync(
            b => b.Id == request.BusinessId,
            cancellationToken);

        if (!businessExists)
        {
            return Result<IReadOnlyList<AccessibilityFeatureDto>>.Failure(
                new Error("Business.NotFound", $"Business '{request.BusinessId}' was not found."),
                Outcome.NotFound);
        }

        var features = await featureRepository.SelectAsync(
            selector: x => AccessibilityFeatureDto.From(x),
            filter: x => x.EntityId == request.BusinessId && x.EntityType == BusinessEntityType,
            orderBy: q => q.OrderBy(x => x.FeatureType).ThenBy(x => x.Name),
            ct: cancellationToken);

        logger.LogInformation(
            "Fetched {Count} accessibility features for Business {BusinessId}",
            features.Count, request.BusinessId);

        return Result<IReadOnlyList<AccessibilityFeatureDto>>.Success(features);
    }
}
