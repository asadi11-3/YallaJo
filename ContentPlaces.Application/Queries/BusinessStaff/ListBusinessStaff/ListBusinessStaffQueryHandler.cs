using ContentPlaces.Application.Queries.BusinessStaff.Common;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Queries.BusinessStaff.ListBusinessStaff;

public sealed class ListBusinessStaffQueryHandler(
    IBusinessStaffRepository staffRepository,
    IBusinessRepository businessRepository,
    ICurrentUser currentUser,
    ILogger<ListBusinessStaffQueryHandler> logger)
    : IQueryHandler<ListBusinessStaffQuery, IReadOnlyList<BusinessStaffDto>>
{
    public async Task<Result<IReadOnlyList<BusinessStaffDto>>> Handle(
        ListBusinessStaffQuery request,
        CancellationToken cancellationToken)
    {
        // Authentication check
        if (!currentUser.IsAuthenticated)
        {
            return Result<IReadOnlyList<BusinessStaffDto>>.Failure(
                Error.Unauthorized("Authentication required"));
        }

        // Validate business existence
        var business = await businessRepository.GetByIdAsync(
            request.BusinessId,
            cancellationToken);

        if (business is null)
        {
            return Result<IReadOnlyList<BusinessStaffDto>>.Failure(
                Error.NotFound("Business.NotFound", "Business not found"));
        }

        // Authorization: owner OR admin-tier role (Admin/SuperAdmin/Owner)
        var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles)
            >= RolePrivilegeLevel.Admin;

        if (!isAdminTier && business.OwnerId != currentUser.UserId)
        {
            return Result<IReadOnlyList<BusinessStaffDto>>.Failure(
                Error.Forbidden("You are not allowed to view this business"));
        }

        // Fetch staff
        var staff = await staffRepository.SelectAsync(
            selector: x => BusinessStaffDto.From(x),
            filter: x => x.BusinessId == request.BusinessId && x.IsActive,
            orderBy: q => q.OrderBy(x => x.Role).ThenBy(x => x.UserId),
            ct: cancellationToken);

        logger.LogInformation(
            "Fetched {Count} staff members for Business {BusinessId}",
            staff.Count, request.BusinessId);

        return Result<IReadOnlyList<BusinessStaffDto>>.Success(staff);
    }
}
