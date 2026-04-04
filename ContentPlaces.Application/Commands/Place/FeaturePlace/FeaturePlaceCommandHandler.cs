using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Exceptions;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Commands.Place.FeaturePlace;

public sealed class FeaturePlaceCommandHandler(
    IPlaceRepository placeRepository,
    IContentPlacesUnitOfWork unitOfWork,
    ILogger<FeaturePlaceCommandHandler> logger)
    : ICommandHandler<FeaturePlaceCommand>
{
    public async Task<Result> Handle(FeaturePlaceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var place = await placeRepository.GetByIdAsync(request.PlaceId, cancellationToken, asNoTracking: false);
            if (place is null)
            {
                return Result.Failure(
                   new Error("Place.NotFound", $"Place '{request.PlaceId}' was not found."),
                   Outcome.NotFound);
            }

            place.SetFeatured(request.IsFeatured);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ContentPlaceConcurrencyException)
            {
                return Result.Failure(
                    new Error(
                        "Place.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            logger.LogInformation(
                "Place {PlaceId} featured={IsFeatured}", request.PlaceId, request.IsFeatured);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
