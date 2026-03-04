
using Security.Contracts.Authorization;
using Security.Domain.Entities;
using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Commands.CreateRole;

public sealed class CreateRoleCommandHandler(
    IRoleRepository roleRepository,
    ISecurityUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : ICommandHandler<CreateRoleCommand, CreateRoleResult>
{
    public async Task<Result<CreateRoleResult>> Handle(CreateRoleCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Result<CreateRoleResult>.Failure(
                Error.Unauthorized("Authentication is required."), Outcome.Unauthorized);

        if (AppRoles.ProtectedRoles.Contains(request.Name, StringComparer.OrdinalIgnoreCase))
            return Result<CreateRoleResult>.Failure(RoleErrors.Protected, Outcome.Forbidden);

        if (await roleRepository.ExistsByNameAsync(request.Name, ct))
            return Result<CreateRoleResult>.Failure(RoleErrors.AlreadyExists, Outcome.Conflict);

        var role = Role.Create(request.Name, request.Description);
        await roleRepository.AddAsync(role, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<CreateRoleResult>.Created(new CreateRoleResult(role.Id, role.Name));
    }
}
