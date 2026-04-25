using Microsoft.Extensions.Caching.Hybrid;
using Security.Application.Authorization;
using Security.Application.Caching;
using Security.Contracts.Authorization;
using Security.Domain.Entities;
using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Commands.CreateRole;

public sealed class CreateRoleCommandHandler(
    IRoleRepository roleRepository,
    ISecurityUnitOfWork unitOfWork,
    IRoleHierarchyService hierarchy,
    HybridCache cache)
    : ICommandHandler<CreateRoleCommand, CreateRoleResult>
{
    public async Task<Result<CreateRoleResult>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        // Hierarchy: creating a role with a reserved/privileged name would let
        // a lower-tier actor smuggle in an elevated role definition. Actor must
        // strictly outrank the privilege level implied by the role name.
        var roleGuard = hierarchy.EnsureCanModifyRoleDefinition(request.Name);
        if (!roleGuard.IsSuccess)
            return Result<CreateRoleResult>.Failure(roleGuard.Errors[0], roleGuard.Outcome);

        if (AppRoles.ProtectedRoles.Contains(request.Name, StringComparer.OrdinalIgnoreCase))
            return Result<CreateRoleResult>.Failure(RoleErrors.Protected, Outcome.Conflict);

        if (await roleRepository.AnyAsync(r => r.Name == request.Name, cancellationToken))
            return Result<CreateRoleResult>.Failure(RoleErrors.AlreadyExists, Outcome.Conflict);

        var role = Role.Create(request.Name, request.Description);
        await roleRepository.AddAsync(role, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(SecurityCacheKeys.RolesTag, cancellationToken);

        return Result<CreateRoleResult>.Created(new CreateRoleResult(role.Id, role.Name));
    }
}
