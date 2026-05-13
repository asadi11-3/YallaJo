using ContentPlaces.Application.Interfaces;
using ContentPlaces.Application.Queries.BusinessStaff.Common;
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
using StaffEntity = ContentPlaces.Domain.Entities.BusinessStaff;

namespace ContentPlaces.Application.Commands.BusinessStaff.AddBusinessStaff;

public sealed class AddBusinessStaffCommandHandler(
    IBusinessStaffRepository staffRepository,
    IBusinessRepository businessRepository,
    IContentPlacesUnitOfWork unitOfWork,
    IContentPlacesOutboxWriter outbox,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<AddBusinessStaffCommandHandler> logger)
    : ICommandHandler<AddBusinessStaffCommand, BusinessStaffDto>
{
    public async Task<Result<BusinessStaffDto>> Handle(
        AddBusinessStaffCommand request,
        CancellationToken cancellationToken)
    {
        // Authentication
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result<BusinessStaffDto>.Failure(
                Error.Unauthorized("Authentication required"));
        }

        // Business existence
        var business = await businessRepository.GetByIdAsync(
            request.BusinessId,
            cancellationToken);

        if (business is null)
        {
            return Result<BusinessStaffDto>.Failure(
                Error.NotFound(
                    "Business.NotFound",
                    "Business not found"));
        }

        // Authorization: owner OR admin-tier role (Admin/SuperAdmin/Owner)
        var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles)
            >= RolePrivilegeLevel.Admin;

        if (!isAdminTier && business.OwnerId != currentUser.UserId)
        {
            return Result<BusinessStaffDto>.Failure(
                Error.Forbidden(
                    "You are not allowed to modify this business"));
        }

        // Duplicate check
        var exists = await staffRepository.AnyAsync(
            x => x.BusinessId == request.BusinessId &&
                 x.UserId == request.UserId &&
                 x.IsActive,
            cancellationToken);

        if (exists)
        {
            return Result<BusinessStaffDto>.Failure(
                Error.Conflict(
                    "BusinessStaff.Duplicate",
                    "User already added"));
        }

        // Create staff entity
        var staff = StaffEntity.Create(
            request.BusinessId,
            request.UserId,
            request.Role);

        await staffRepository.AddAsync(staff, cancellationToken);

        // BusinessStaff is a non-aggregate child of Business; integration events are
        // staged manually through IContentPlacesOutboxWriter (this is the canonical
        // path — same pattern as ServiceItem).  The outbox row commits atomically
        // with the staff row inside the same UnitOfWork.SaveChangesAsync call below.
        outbox.Enqueue(new BusinessStaffAddedIntegrationEvent(
            staff.Id,
            staff.BusinessId,
            staff.UserId,
            staff.Role.ToString()));

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<BusinessStaffDto>.Failure(
                Error.Conflict(
                    "BusinessStaff.ConcurrencyConflict",
                    "A concurrency conflict occurred. Please refresh and try again."));
        }

        // Evict scoped business tag so ListBusinessStaffQuery returns fresh data.
        await cache.RemoveByTagAsync(ContentPlacesCacheKeys.BusinessTag(request.BusinessId), cancellationToken);

        logger.LogInformation(
            "Staff {StaffId} created: User {UserId} as {Role} in Business {BusinessId}",
            staff.Id,
            request.UserId,
            request.Role,
            request.BusinessId);

        return Result<BusinessStaffDto>.Created(
            BusinessStaffDto.From(staff));
    }
}
