using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Exceptions;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using AccessibilityFeatureEntity = ContentPlaces.Domain.Entities.AccessibilityFeature;

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
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ContentPlaceConcurrencyException)
        {
            return Result.Failure(
                new Error(
                    "AccessibilityFeature.ConcurrencyConflict",
                    "A concurrency conflict occurred. Please refresh and try again."),
                Outcome.Conflict);
        }

        logger.LogInformation(
            "Accessibility updated for Place {PlaceId}. New count: {Count}",
            request.PlaceId, newFeatures.Count);

        return Result.Success();
    }
}
