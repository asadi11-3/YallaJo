using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Commands.Place.UpdatePlace;

public sealed class UpdatePlaceCommandHandler(
    IPlaceRepository placeRepository,
    IContentPlacesUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<UpdatePlaceCommandHandler> logger)
    : ICommandHandler<UpdatePlaceCommand, UpdatePlaceResult>
{
    public async Task<Result<UpdatePlaceResult>> Handle(
        UpdatePlaceCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            {
                return Result<UpdatePlaceResult>.Failure(
                    Error.Unauthorized("Authentication is required."),
                    Outcome.Unauthorized);
            }

            var place = await placeRepository.GetByIdAsync(request.Id, cancellationToken, asNoTracking: false);
            if (place is null)
            {
                return Result<UpdatePlaceResult>.Failure(
                   new Error("Place.NotFound", $"Place '{request.Id}' was not found."),
                   Outcome.NotFound);
            }

            // Owner-or-admin-tier authorization (IDOR prevention).
            // Endpoint already verified Place.Update permission; per-row ownership is enforced here.
            var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles)
                >= RolePrivilegeLevel.Admin;
            if (!isAdminTier && place.CreatedByUserId != currentUser.UserId.Value)
            {
                return Result<UpdatePlaceResult>.Failure(
                    Error.Forbidden("You do not have permission to update this place."),
                    Outcome.Forbidden);
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
            catch (DbUpdateConcurrencyException)
            {
                return Result<UpdatePlaceResult>.Conflict(
                    new Error(
                        "Place.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."));
            }

            await cache.RemoveByTagAsync($"place:{place.Id}", cancellationToken);
            await cache.RemoveByTagAsync("places", cancellationToken);

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
