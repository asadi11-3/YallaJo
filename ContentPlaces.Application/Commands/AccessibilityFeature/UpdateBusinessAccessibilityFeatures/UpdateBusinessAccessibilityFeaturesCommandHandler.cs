using ContentPlaces.Application.Caching;
using ContentPlaces.Application.Commands.AccessibilityFeature.UpdateAccessibilityFeatures;
using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using AccessibilityFeatureEntity = ContentPlaces.Domain.Entities.AccessibilityFeature;

namespace ContentPlaces.Application.Commands.AccessibilityFeature.UpdateBusinessAccessibilityFeatures;

public sealed class UpdateBusinessAccessibilityFeaturesCommandHandler(
    IAccessibilityFeatureRepository featureRepository,
    IBusinessRepository businessRepository,
    ICurrentUser currentUser,
    IContentPlacesUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<UpdateBusinessAccessibilityFeaturesCommandHandler> logger)
    : ICommandHandler<UpdateBusinessAccessibilityFeaturesCommand>
{
    private const byte BusinessEntityType = 2;

    public async Task<Result> Handle(
        UpdateBusinessAccessibilityFeaturesCommand request,
        CancellationToken cancellationToken)
    {
        var business = await businessRepository.GetByIdAsync(request.BusinessId, cancellationToken, asNoTracking: false);
        if (business is null)
        {
            return Result.Failure(Error.NotFound("Business.NotFound", "Business not found"));
        }

        // Only the owner (or admin via MustHavePermission at endpoint level) can update
        if (business.OwnerId != currentUser.UserId!.Value)
        {
            return Result.Failure(
                new Error("Business.Unauthorized", "You do not own this business."),
                Outcome.Forbidden);
        }

        // Get existing features for this business
        var existingFeatures = await featureRepository.GetAllAsync(
            filter: x => x.EntityId == request.BusinessId && x.EntityType == BusinessEntityType,
            ct: cancellationToken);

        // Remove old, create new (deduplicate by FeatureType)
        featureRepository.RemoveRange(existingFeatures);

        var newFeatures = request.Features
            .GroupBy(x => x.FeatureType)
            .Select(x => x.First())
            .Select(x => AccessibilityFeatureEntity.Create(
                BusinessEntityType,
                request.BusinessId,
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

        await cache.RemoveByTagAsync(ContentPlacesCacheKeys.BusinessTag(request.BusinessId), cancellationToken);

        logger.LogInformation(
            "Accessibility updated for Business {BusinessId}. New count: {Count}",
            request.BusinessId, newFeatures.Count);

        return Result.Success();
    }
}
