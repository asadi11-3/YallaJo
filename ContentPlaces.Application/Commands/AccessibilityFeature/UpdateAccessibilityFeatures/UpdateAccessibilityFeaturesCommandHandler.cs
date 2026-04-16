using ContentPlaces.Application.Interfaces;
using AccessibilityFeatureEntity = ContentPlaces.Domain.Entities.AccessibilityFeature;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Commands.AccessibilityFeature.UpdateAccessibilityFeatures;

public sealed class UpdateAccessibilityFeaturesCommandHandler(
    IContentPlacesDbContext dbContext,
    IContentPlacesUnitOfWork unitOfWork,
    ILogger<UpdateAccessibilityFeaturesCommandHandler> logger)
    : ICommandHandler<UpdateAccessibilityFeaturesCommand>
{
    private const byte PlaceEntityType = 1; // polymorphic:  Place  حالياً بس 

    public async Task<Result> Handle(
        UpdateAccessibilityFeaturesCommand request,
        CancellationToken cancellationToken)
    {
        // check place exists
        var exists = await dbContext.Places
            .AsNoTracking()
            .AnyAsync(x => x.Id == request.PlaceId, cancellationToken);

        if (!exists)
        {
            return Result.Failure(
                new Error("Place.NotFound", "Place not found"),
                Outcome.NotFound);
        }

        // get old features
        var oldFeatures = await dbContext.AccessibilityFeatures
            .Where(x => x.EntityId == request.PlaceId && x.EntityType == PlaceEntityType)
            .ToListAsync(cancellationToken);

        // remove old

        var existingFeatures = await dbContext.AccessibilityFeatures
    .Where(x => x.EntityId == request.PlaceId && x.EntityType == PlaceEntityType)
    .ToListAsync(cancellationToken);

        dbContext.AccessibilityFeatures.RemoveRange(existingFeatures); 

        // create new
        var newFeatures = request.Features
            .GroupBy(x => x.FeatureType) // prevent duplicates
            .Select(x => x.First())
            .Select(x => AccessibilityFeatureEntity.Create(
                PlaceEntityType,
                request.PlaceId,
                x.FeatureType,
                x.Name,
                x.Description,
                x.IsAvailable))
            .ToList();

        await dbContext.AccessibilityFeatures.AddRangeAsync(newFeatures, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Accessibility updated for Place {PlaceId}. New count: {Count}",
            request.PlaceId,
            newFeatures.Count);

        return Result.Success();
    }
}
