using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Exceptions;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Commands.Place.UpdatePlace;

public sealed class UpdatePlaceCommandHandler(
    IPlaceRepository placeRepository,
    IContentPlacesUnitOfWork unitOfWork,
    ILogger<UpdatePlaceCommandHandler> logger)
    : ICommandHandler<UpdatePlaceCommand, UpdatePlaceResult>
{
    public async Task<Result<UpdatePlaceResult>> Handle(
        UpdatePlaceCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var place = await placeRepository.GetByIdAsync(request.Id, cancellationToken, asNoTracking: false);
            if (place is null)
            {
                return Result<UpdatePlaceResult>.Failure(
                   new Error("Place.NotFound", $"Place '{request.Id}' was not found."),
                   Outcome.NotFound);
            }

            if (await placeRepository.AnyAsync(p => p.Slug == request.Slug && p.Id != request.Id, cancellationToken))
            {
                return Result<UpdatePlaceResult>.Conflict(
                    new Error("Place.SlugConflict", $"A place with slug '{request.Slug}' already exists."));
            }

            place.Update(
                name:            request.Name,
                slug:            request.Slug,
                placeType:       request.PlaceType,
                latitude:        request.Latitude,
                longitude:       request.Longitude,
                description:     request.Description,
                address:         request.Address,
                city:            request.City,
                country:         request.Country,
                postalCode:      request.PostalCode,
                phone:           request.Phone,
                email:           request.Email,
                website:         request.Website,
                metaTitle:       request.MetaTitle,
                metaDescription: request.MetaDescription);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ContentPlaceConcurrencyException)
            {
                return Result<UpdatePlaceResult>.Conflict(
                    new Error(
                        "Place.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."));
            }

            logger.LogInformation("Place updated: {PlaceId}", place.Id);

            return Result<UpdatePlaceResult>.Success(
                new UpdatePlaceResult(place.Id, place.Name, place.Slug));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<UpdatePlaceResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
