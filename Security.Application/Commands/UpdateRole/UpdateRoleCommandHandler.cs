using Microsoft.Extensions.Caching.Hybrid;
using Security.Application.Caching;
using Security.Contracts.Authorization;
using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Commands.UpdateRole;

public sealed class UpdateRoleCommandHandler(
    IRoleRepository roleRepository,
    ISecurityUnitOfWork unitOfWork,
    HybridCache cache)
    : ICommandHandler<UpdateRoleCommand>
{
    public async Task<Result> Handle(UpdateRoleCommand request, CancellationToken ct)
    {
        var role = await roleRepository.GetByIdAsync(request.RoleId, ct, asNoTracking: false);
        if (role is null)
            return Result.Failure(RoleErrors.NotFound, Outcome.NotFound);

        if (AppRoles.ProtectedRoles.Contains(role.Name, StringComparer.OrdinalIgnoreCase))
            return Result.Failure(RoleErrors.Protected, Outcome.Forbidden);

        role.UpdateDescription(request.Description);
        await unitOfWork.SaveChangesAsync(ct);

        await cache.RemoveByTagAsync(SecurityCacheKeys.RolesTag, ct);

        return Result.Success();
    }
}
