using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Exceptions;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Commands.Place.DeletePlace;

public sealed class DeletePlaceCommandHandler(
    IPlaceRepository placeRepository,
    IContentPlacesUnitOfWork unitOfWork,
    ILogger<DeletePlaceCommandHandler> logger)
    : ICommandHandler<DeletePlaceCommand>
{
    public async Task<Result> Handle(DeletePlaceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var place = await placeRepository.GetByIdAsync(request.PlaceId, cancellationToken, asNoTracking: false);
            if (place is null) {
                return Result.Failure(
                    new Error("Place.NotFound", $"Place '{request.PlaceId}' was not found."),
                    Outcome.NotFound);
            }

            if (await placeRepository.AnyAsync(pb => pb.Id == request.PlaceId, cancellationToken))
            {
                return Result.Failure(
                  new Error(
                      "Place.HasActiveBusinesses",
                      "Cannot delete a place that has active businesses linked to it."),
                  Outcome.Invalid);
            }

            place.SoftDelete();

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

            logger.LogInformation("Place soft-deleted: {PlaceId}", request.PlaceId);

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
