using Security.Domain.Entities;
using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Commands.AddRoleClaim;

public sealed class AddRoleClaimCommandHandler(
    IRoleRepository roleRepository,
    IRoleClaimRepository roleClaimRepository,
    ISecurityUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : ICommandHandler<AddRoleClaimCommand>
{
    public async Task<Result> Handle(AddRoleClaimCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result.Failure(Error.Unauthorized("Authentication is required."), Outcome.Unauthorized);

        var role = await roleRepository.GetByIdAsync(request.RoleId, ct);
        if (role is null)
            return Result.Failure(RoleErrors.NotFound, Outcome.NotFound);

        if (await roleClaimRepository.ExistsAsync(request.RoleId, request.ClaimType, request.ClaimValue, ct))
            return Result.Failure(
                new Error("RoleClaim.Duplicate", "This claim already exists on the role."),
                Outcome.Conflict);

        var claim = RoleClaim.Create(request.RoleId, request.ClaimType, request.ClaimValue);
        await roleClaimRepository.AddAsync(claim, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
