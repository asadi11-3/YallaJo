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
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            {
                return Result.Failure(
                    Error.Unauthorized("Authentication is required."),
                    Outcome.Unauthorized);
            }

            var place = await placeRepository.GetByIdAsync(request.PlaceId, cancellationToken, asNoTracking: false);
            if (place is null)
            {
                return Result.Failure(
                    new Error("Place.NotFound", $"Place '{request.PlaceId}' was not found."),
                    Outcome.NotFound);
            }

            // Owner-or-admin-tier authorization (IDOR prevention).
            var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles)
                >= RolePrivilegeLevel.Admin;
            if (!isAdminTier && place.CreatedByUserId != currentUser.UserId.Value)
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

            await cache.RemoveByTagAsync($"place:{request.PlaceId}", cancellationToken);
            await cache.RemoveByTagAsync("places", cancellationToken);

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
