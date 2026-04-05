using Microsoft.Extensions.Caching.Hybrid;
using Security.Application.Caching;
using Security.Contracts.Authorization;
using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Commands.AssignRole;

public sealed class AssignRoleCommandHandler(
    IRoleRepository roleRepository,
    IUserRepository userRepository,
    ISecurityUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache)
    : ICommandHandler<AssignRoleCommand>
{
    public async Task<Result> Handle(AssignRoleCommand request, CancellationToken ct)
    {
        var role = await roleRepository.GetByIdAsync(request.RoleId, ct);
        if (role is null)
            return Result.Failure(RoleErrors.NotFound, Outcome.NotFound);

        if (!role.IsActive)
            return Result.Failure(RoleErrors.Inactive, Outcome.Invalid);

        // OwnerOnly: only the Owner can assign Owner/SuperAdmin roles.
        if (AppRoles.OwnerOnlyRoles.Contains(role.Name, StringComparer.Ordinal)
            && !currentUser.IsInRole(AppRoles.Owner))
            return Result.Failure(RoleErrors.OwnerOnly, Outcome.Forbidden);

        // OwnerSingleton: only one user may hold the Owner role.
        if (role.Name == AppRoles.Owner
            && await userRepository.AnyWithRoleAsync(AppRoles.Owner, ct))
            return Result.Failure(RoleErrors.OwnerSingleton, Outcome.Conflict);

        var user = await userRepository.GetByIdAsync(request.UserId, ct, asNoTracking: false);
        if (user is null)
            return Result.Failure(UserErrors.NotFound, Outcome.NotFound);

        var existing = await userRepository.GetUserRoleAsync(request.UserId, request.RoleId, ct);
        if (existing is not null)
            return Result.Failure(RoleErrors.AlreadyAssigned, Outcome.Conflict);

        user.AssignRole(role);
        await unitOfWork.SaveChangesAsync(ct);

        await cache.RemoveByTagAsync(SecurityCacheKeys.UserTag(request.UserId), ct);
        await cache.RemoveByTagAsync(SecurityCacheKeys.UsersTag, ct);

        return Result.Success();
    }
}
