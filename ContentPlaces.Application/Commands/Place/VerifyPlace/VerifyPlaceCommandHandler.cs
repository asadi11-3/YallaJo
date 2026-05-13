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

namespace ContentPlaces.Application.Commands.Place.VerifyPlace;

public sealed class VerifyPlaceCommandHandler(
    IPlaceRepository placeRepository,
    IContentPlacesUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<VerifyPlaceCommandHandler> logger)
    : ICommandHandler<VerifyPlaceCommand>
{
    public async Task<Result> Handle(VerifyPlaceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            {
                return Result.Failure(
                    Error.Unauthorized("Authentication is required."),
                    Outcome.Unauthorized);
            }

            // Admin-tier only — endpoint summary documents this as "admin only".
            var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles)
                >= RolePrivilegeLevel.Admin;
            if (!isAdminTier)
            {
                return Result.Failure(
                    Error.Forbidden("You do not have permission to verify places."),
                    Outcome.Forbidden);
            }

            var place = await placeRepository.GetByIdAsync(request.PlaceId, cancellationToken, asNoTracking: false);
            if (place is null)
            {
                return Result.Failure(
                   new Error("Place.NotFound", $"Place '{request.PlaceId}' was not found."),
                   Outcome.NotFound);
            }

            place.SetVerified(request.IsVerified);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error(
                        "Place.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(ContentPlacesCacheKeys.TagForPlace(request.PlaceId), cancellationToken);
            await cache.RemoveByTagAsync(ContentPlacesCacheKeys.TagPlaces, cancellationToken);

            logger.LogInformation(
                "Place {PlaceId} verified={IsVerified}", request.PlaceId, request.IsVerified);

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
