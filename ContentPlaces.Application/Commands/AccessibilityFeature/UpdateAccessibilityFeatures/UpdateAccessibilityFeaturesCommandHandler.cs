using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using AccessibilityFeatureEntity = ContentPlaces.Domain.Entities.AccessibilityFeature;

namespace ContentPlaces.Application.Commands.AccessibilityFeature.UpdateAccessibilityFeatures;

public sealed class UpdateAccessibilityFeaturesCommandHandler(
    IAccessibilityFeatureRepository featureRepository,
    IPlaceRepository placeRepository,
    IContentPlacesUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<UpdateAccessibilityFeaturesCommandHandler> logger)
    : ICommandHandler<UpdateAccessibilityFeaturesCommand>
{
    private const byte PlaceEntityType = 1;

    public async Task<Result> Handle(
        UpdateAccessibilityFeaturesCommand request,
        CancellationToken cancellationToken)
    {
        // Authentication check
        if (!currentUser.IsAuthenticated)
        {
            return Result.Failure(
                Error.Unauthorized("Authentication required"));
        }

        // Authorization: Admin only
        if (!currentUser.IsInRole("Admin"))
        {
            return Result.Failure(
                Error.Forbidden("Admin access required"));
        }

        // Validate place existence
        var placeExists = await placeRepository.AnyAsync(
            x => x.Id == request.PlaceId,
            cancellationToken);

        if (!placeExists)
        {
            return Result.Failure(
                Error.NotFound("Place.NotFound", "Place not found"));
        }

        // Get existing features
        var existingFeatures = await featureRepository.GetAllAsync(
            filter: x => x.EntityId == request.PlaceId && x.EntityType == PlaceEntityType,
            ct: cancellationToken);

        // Remove old features
        featureRepository.RemoveRange(existingFeatures);

        // Create new features (deduplicate by FeatureType)
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
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(
                Error.Conflict(
                    "AccessibilityFeature.ConcurrencyConflict",
                    "A concurrency conflict occurred. Please refresh and try again."));
        }

        logger.LogInformation(
            "Accessibility updated for Place {PlaceId}. New count: {Count}",
            request.PlaceId, newFeatures.Count);

        return Result.Success();
    }
}
