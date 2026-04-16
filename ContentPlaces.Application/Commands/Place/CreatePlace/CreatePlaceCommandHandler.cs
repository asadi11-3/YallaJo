using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Exceptions;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Logging;
using System.Threading;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using PlaceEntity = ContentPlaces.Domain.Entities.Place;

namespace ContentPlaces.Application.Commands.Place.CreatePlace;

public sealed class CreatePlaceCommandHandler(
    IPlaceRepository placeRepository,
    IContentPlacesUnitOfWork unitOfWork,
    ILogger<CreatePlaceCommandHandler> logger)
    : ICommandHandler<CreatePlaceCommand, CreatePlaceResult>
{
    public async Task<Result<CreatePlaceResult>> Handle(
        CreatePlaceCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var slug = string.IsNullOrWhiteSpace(request.Slug)
                ? PlaceEntity.GenerateSlug(request.Name)
                : request.Slug.Trim().ToLowerInvariant();

            if (await placeRepository.AnyAsync(t => t.Slug == request.Slug, cancellationToken))
            {
                return Result<CreatePlaceResult>.Conflict(
                    new Error("Place.SlugConflict", $"A place with slug '{slug}' already exists."));
            }

            var place = PlaceEntity.Create(
                name:            request.Name,
                slug:            slug,
                placeType:       request.PlaceType,
                latitude:        request.Latitude,
                longitude:       request.Longitude,
                createdByUserId: request.CreatedByUserId,
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

            await placeRepository.AddAsync(place, cancellationToken);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ContentPlaceConcurrencyException)
            {
                return Result<CreatePlaceResult>.Conflict(
                    new Error(
                        "Place.ConcurrencyConflict",
                        "A concurrency conflict occurred. Please try again."));
            }

            logger.LogInformation(
                "Place created: {PlaceId} (Slug={Slug})", place.Id, place.Slug);

            return Result<CreatePlaceResult>.Created(
                new CreatePlaceResult(place.Id, place.Name, place.Slug));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<CreatePlaceResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
