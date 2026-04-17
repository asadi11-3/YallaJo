using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Repositories;
using AccessibilityFeatureEntity = ContentPlaces.Domain.Entities.AccessibilityFeature;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Commands.AccessibilityFeature.UpdateAccessibilityFeatures;

public sealed class UpdateAccessibilityFeaturesCommandHandler(
    IAccessibilityFeatureRepository featureRepository,
    IPlaceRepository placeRepository,
    IContentPlacesUnitOfWork unitOfWork,
    ILogger<UpdateAccessibilityFeaturesCommandHandler> logger)
    : ICommandHandler<UpdateAccessibilityFeaturesCommand>
{
    private const byte PlaceEntityType = 1;

    public async Task<Result> Handle(
        UpdateAccessibilityFeaturesCommand request,
        CancellationToken cancellationToken)
    {
        var placeExists = await placeRepository.AnyAsync(
            x => x.Id == request.PlaceId, cancellationToken);

        if (!placeExists)
        {
            return Result.Failure(
                new Error("Place.NotFound", "Place not found"),
                Outcome.NotFound);
        }

        var existingFeatures = await featureRepository.GetAllAsync(
            filter: x => x.EntityId == request.PlaceId && x.EntityType == PlaceEntityType,
            ct: cancellationToken);

        featureRepository.RemoveRange(existingFeatures);

        var newFeatures = request.Features
            .GroupBy(x => x.FeatureType)
            .Select(x => x.First())
            .Select(x => AccessibilityFeatureEntity.Create(
                PlaceEntityType,
                request.PlaceId,
                x.FeatureType,
                x.Name,
                x.Description,
                x.IsAvailable))
            .ToList();

        await featureRepository.AddRangeAsync(newFeatures, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Accessibility updated for Place {PlaceId}. New count: {Count}",
            request.PlaceId, newFeatures.Count);

        return Result.Success();
    }
}
