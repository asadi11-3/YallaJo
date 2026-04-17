using Security.Application.Queries.Dtos;
using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Queries.GetRole;

public sealed class GetRoleQueryHandler(IRoleRepository roleRepository)
    : IQueryHandler<GetRoleQuery, RoleDetailsDto>
{
    public async Task<Result<RoleDetailsDto>> Handle(GetRoleQuery request, CancellationToken ct)
    {
        var role = await roleRepository.GetByIdWithClaimsAsync(request.RoleId, ct);

        if (role is null)
            return Result<RoleDetailsDto>.Failure(RoleErrors.NotFound, Outcome.NotFound);

        var claims = role.RoleClaims
            .Select(rc => new RoleClaimDto(rc.Id, rc.ClaimType, rc.ClaimValue))
            .ToList();

        return Result<RoleDetailsDto>.Success(new RoleDetailsDto(
            role.Id,
            role.Name,
            role.Description,
            role.IsActive,
            claims));
    }
}
