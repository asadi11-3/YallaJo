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

namespace ContentPlaces.Application.Commands.Place.DeletePlace;

public sealed class DeletePlaceCommandHandler(
    IPlaceRepository placeRepository,
    IContentPlacesUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<DeletePlaceCommandHandler> logger)
    : ICommandHandler<DeletePlaceCommand>
{
    public async Task<Result> Handle(DeletePlaceCommand request, CancellationToken cancellationToken)
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

            // Ownership check (IDOR prevention).
            // P1 DeleteOwn/DeleteAny (2026-05-30): owner OR admin-tier may delete.
            // DeleteOwn is owner-scoped; Admin+ override via Place.DeleteAny / admin role tier.
            var isAdminTier =
                AppRoles.HighestPrivilegeLevel(currentUser.Roles) >= RolePrivilegeLevel.Admin
                || currentUser.HasPermission("Permission.Place.DeleteAny");

            if (!isAdminTier && place.CreatedByUserId != currentUser.UserId!.Value)
            {
                return Result.Failure(
                    Error.Forbidden("You do not have permission to delete this place."),
                    Outcome.Forbidden);
            }

            if (await placeRepository.HasActiveLinkedBusinessesAsync(request.PlaceId, cancellationToken))
            {
                return Result.Failure(
                    new Error(
                        "Place.HasActiveBusinesses",
                        "Cannot delete a place that has active businesses linked to it."),
                    Outcome.Invalid);
            }

            place.Delete();

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
