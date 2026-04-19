using Microsoft.Extensions.Caching.Hybrid;
using Security.Application.Caching;
using Security.Contracts.Authorization;
using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Commands.DeactivateRole;

public sealed class DeactivateRoleCommandHandler(
    IRoleRepository roleRepository,
    ISecurityUnitOfWork unitOfWork,
    HybridCache cache)
    : ICommandHandler<DeactivateRoleCommand>
{
    public async Task<Result> Handle(DeactivateRoleCommand request, CancellationToken ct)
    {
        var role = await roleRepository.GetByIdAsync(request.RoleId, ct, asNoTracking: false);
        if (role is null)
            return Result.Failure(RoleErrors.NotFound, Outcome.NotFound);

       
        if (AppRoles.ProtectedRoles.Contains(role.Name, StringComparer.OrdinalIgnoreCase))
            return Result.Failure(RoleErrors.Protected, Outcome.Conflict);

        if (!role.IsActive)
            return Result.Success(); // idempotent — already deactivated

        role.Deactivate();
        await unitOfWork.SaveChangesAsync(ct);

        // Invalidate roles list AND all user caches: GetUserQuery filters by ur.Role.IsActive,
        // so deactivating a role changes which roles appear in cached UserDto.Roles.
        await cache.RemoveByTagAsync(SecurityCacheKeys.RolesTag, ct);
        await cache.RemoveByTagAsync(SecurityCacheKeys.UsersTag, ct);

        return Result.Success();
    }
}
