using ContentPlaces.Application.Interfaces;
using ContentPlaces.Application.Caching;
using ContentPlaces.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Commands.BusinessAmenity.RemoveBusinessAmenity;

public sealed class RemoveBusinessAmenityCommandHandler(
    IBusinessAmenityRepository amenityRepository,
    IContentPlacesUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<RemoveBusinessAmenityCommandHandler> logger)
    : ICommandHandler<RemoveBusinessAmenityCommand>
{
    public async Task<Result> Handle(
        RemoveBusinessAmenityCommand request,
        CancellationToken cancellationToken)
    {
        // Authentication check
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result.Failure(
                Error.Unauthorized("Authentication required"));
        }

        // Get amenity with Business included
        var amenity = await amenityRepository.GetByIdWithBusinessAsync(
            request.AmenityId,
            cancellationToken);

        if (amenity is null)
        {
            return Result.Failure(
                Error.NotFound(
                    "BusinessAmenity.NotFound",
                    "Amenity not found"));
        }

        // Authorization: owner OR admin-tier role (Admin/SuperAdmin/Owner)
        var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles)
            >= RolePrivilegeLevel.Admin;

        if (!isAdminTier && amenity.Business.OwnerId != currentUser.UserId)
        {
            return Result.Failure(
                Error.Forbidden(
                    "You are not allowed to modify this business"));
        }

        // Remove amenity
        amenityRepository.Remove(amenity);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(
                Error.Conflict(
                    "BusinessAmenity.ConcurrencyConflict",
                    "A concurrency conflict occurred. Please refresh and try again."));
        }

        // Evict scoped business tag so ListBusinessAmenitiesQuery returns fresh data.
        await cache.RemoveByTagAsync(ContentPlacesCacheKeys.BusinessTag(amenity.BusinessId), cancellationToken);

        logger.LogInformation(
            "Amenity removed successfully. AmenityId: {AmenityId}",
            request.AmenityId);

        return Result.Success();
    }
}
