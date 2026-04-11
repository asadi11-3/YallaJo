using Microsoft.Extensions.Caching.Hybrid;
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
    HybridCache cache)
    : ICommandHandler<CreateRoleCommand, CreateRoleResult>
{
    public async Task<Result<CreateRoleResult>> Handle(CreateRoleCommand request, CancellationToken ct)
    {
        // Authentication and permission (Role.Create) are enforced by the endpoint.
        // This handler does not act on the caller's identity — no ICurrentUser needed.
        if (AppRoles.ProtectedRoles.Contains(request.Name, StringComparer.OrdinalIgnoreCase))
            return Result<CreateRoleResult>.Failure(RoleErrors.Protected, Outcome.Forbidden);

        if (await roleRepository.AnyAsync(r => r.Name == request.Name, ct))
            return Result<CreateRoleResult>.Failure(RoleErrors.AlreadyExists, Outcome.Conflict);

        var role = Role.Create(request.Name, request.Description);
        await roleRepository.AddAsync(role, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await cache.RemoveByTagAsync(SecurityCacheKeys.RolesTag, ct);

        return Result<CreateRoleResult>.Created(new CreateRoleResult(role.Id, role.Name));
    }
}
