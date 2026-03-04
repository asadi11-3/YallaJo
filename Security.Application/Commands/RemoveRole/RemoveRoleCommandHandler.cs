
using Security.Contracts.Authorization;
using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Commands.RemoveRole;

public sealed class RemoveRoleCommandHandler(
    IRoleRepository roleRepository,
    IUserRepository userRepository,
    ISecurityUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : ICommandHandler<RemoveRoleCommand>
{
    public async Task<Result> Handle(RemoveRoleCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Result.Failure(Error.Unauthorized("Authentication is required."), Outcome.Unauthorized);

        var role = await roleRepository.GetByIdAsync(request.RoleId, ct);
        if (role is null)
            return Result.Failure(RoleErrors.NotFound, Outcome.NotFound);

        // Cannot remove users from protected roles.
        if (AppRoles.ProtectedRoles.Contains(role.Name, StringComparer.OrdinalIgnoreCase))
            return Result.Failure(RoleErrors.Protected, Outcome.Forbidden);

        // OwnerOnly: only the Owner can remove Owner/SuperAdmin roles.
        if (AppRoles.OwnerOnlyRoles.Contains(role.Name, StringComparer.Ordinal)
            && !currentUser.IsInRole(AppRoles.Owner))
            return Result.Failure(RoleErrors.OwnerOnly, Outcome.Forbidden);

        var userRole = await userRepository.GetUserRoleAsync(request.UserId, request.RoleId, ct);
        if (userRole is null)
            return Result.Failure(RoleErrors.NotAssigned, Outcome.NotFound);

        userRepository.RemoveUserRole(userRole);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
