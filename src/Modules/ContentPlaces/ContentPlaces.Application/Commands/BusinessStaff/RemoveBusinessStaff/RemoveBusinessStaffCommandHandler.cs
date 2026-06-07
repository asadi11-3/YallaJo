using ContentPlaces.Application.Interfaces;
using ContentPlaces.Application.Caching;
using ContentPlaces.Contracts.BusinessStaff;
using ContentPlaces.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Commands.BusinessStaff.RemoveBusinessStaff;

public sealed class RemoveBusinessStaffCommandHandler(
    IBusinessStaffRepository staffRepository,
    IContentPlacesUnitOfWork unitOfWork,
    IContentPlacesOutboxWriter outbox,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<RemoveBusinessStaffCommandHandler> logger)
    : ICommandHandler<RemoveBusinessStaffCommand>
{
    public async Task<Result> Handle(
        RemoveBusinessStaffCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            return Result.Failure(
                Error.Unauthorized("Authentication required."),
                Outcome.Unauthorized);
        }

        var staff = await staffRepository.GetByIdWithBusinessAsync(
            request.Id,
            asNoTracking: false,
            ct: cancellationToken);

        if (staff is null)
        {
            return Result.Failure(
                Error.NotFound("BusinessStaff.NotFound", "Staff not found"));
        }

        // Ownership check (IDOR prevention).
        var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles) >= RolePrivilegeLevel.Admin;

        if (!isAdminTier && staff.Business.OwnerId != currentUser.UserId.Value)
        {
            return Result.Failure(
                Error.Forbidden("You are not allowed to modify this business"));
        }

        // already inactive
        if (!staff.IsActive)
        {
            return Result.Failure(
                Error.Conflict("BusinessStaff.AlreadyInactive", "Staff already deactivated"));
        }

        // deactivate
        staff.Deactivate();

        // BusinessStaff is a non-aggregate child of Business; integration events are
        // staged manually through IContentPlacesOutboxWriter (this is the canonical
        // path — same pattern as ServiceItem).  The outbox row commits atomically
        // with the deactivation inside the same UnitOfWork.SaveChangesAsync call below.
        outbox.Enqueue(new BusinessStaffRemovedIntegrationEvent(
            staff.Id,
            staff.BusinessId,
            staff.UserId));

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(
                Error.Conflict(
                    "BusinessStaff.ConcurrencyConflict",
                    "A concurrency conflict occurred. Please refresh and try again."));
        }

        // Evict scoped business tag so ListBusinessStaffQuery returns fresh data.
        await cache.RemoveByTagAsync(ContentPlacesCacheKeys.BusinessTag(staff.BusinessId), cancellationToken);

        logger.LogInformation("Staff {StaffId} deactivated", request.Id);

        return Result.Success();
    }
}
