using ContentPlaces.Application.Caching;
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
            if (currentUser.UserId is null)
            {
                return Result<UpdatePlaceResult>.Failure(
                    Error.Unauthorized("Authentication required."),
                    Outcome.Unauthorized);
            }

            var place = await placeRepository.GetByIdAsync(request.Id, cancellationToken, asNoTracking: false);
            if (place is null)
            {
                return Result<UpdatePlaceResult>.Failure(
                   new Error("Place.NotFound", $"Place '{request.Id}' was not found."),
                   Outcome.NotFound);
            }

            // Ownership check (IDOR prevention).
            var isAdminTier =
                AppRoles.HighestPrivilegeLevel(currentUser.Roles) >= RolePrivilegeLevel.Admin;

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

            // CONTENTPLACES-FOLLOWUP-CACHE-SLUG-001: capture the old slug BEFORE
            // the in-memory update so we can evict the stale per-slug cache entry
            // after a successful save.  Mirrors the ContentTours P1-005 standard.
            var oldSlug = place.Slug;
            var newSlug = request.Slug;

            place.Update(
                name:            request.Name,
                slug:            newSlug,
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

            // Cache invalidation strictly AFTER successful SaveChanges.  Failure
            // paths above (NotFound / Forbidden / SlugConflict /
            // ConcurrencyConflict) return early and never invalidate.
            await cache.RemoveByTagAsync(ContentPlacesCacheKeys.TagForPlace(place.Id), cancellationToken);
            await cache.RemoveByTagAsync(ContentPlacesCacheKeys.TagPlaces, cancellationToken);

            // CONTENTPLACES-FOLLOWUP-CACHE-SLUG-001: per-slug eviction.  Always
            // evict the new slug tag; additionally evict the old slug tag when
            // the slug actually changed so the stale slug entry cannot serve
            // the now-renamed Place.
            await cache.RemoveByTagAsync(ContentPlacesCacheKeys.TagForPlaceSlug(newSlug), cancellationToken);
            if (!string.Equals(oldSlug, newSlug, StringComparison.OrdinalIgnoreCase))
            {
                await cache.RemoveByTagAsync(ContentPlacesCacheKeys.TagForPlaceSlug(oldSlug), cancellationToken);
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
