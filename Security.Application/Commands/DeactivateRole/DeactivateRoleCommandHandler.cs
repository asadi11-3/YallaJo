using Security.Contracts.Authorization;
using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Commands.DeactivateRole;

public sealed class DeactivateRoleCommandHandler(
    IRoleRepository roleRepository,
    ISecurityUnitOfWork unitOfWork
  )
    : ICommandHandler<DeactivateRoleCommand>
{
    public async Task<Result> Handle(DeactivateRoleCommand request, CancellationToken ct)
    {

        var role = await roleRepository.GetByIdAsync(request.RoleId, ct, asNoTracking: false);
        if (role is null)
            return Result.Failure(RoleErrors.NotFound, Outcome.NotFound);

        if (AppRoles.ProtectedRoles.Contains(role.Name, StringComparer.OrdinalIgnoreCase))
            return Result.Failure(RoleErrors.Protected, Outcome.Forbidden);

        if (!role.IsActive)
            return Result.Success(); // idempotent — already deactivated

        role.Deactivate();
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
