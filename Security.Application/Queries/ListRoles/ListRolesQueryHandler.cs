using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Queries.ListRoles;

public sealed class ListRolesQueryHandler(IRoleRepository roleRepository)
    : IQueryHandler<ListRolesQuery, IReadOnlyList<RoleDto>>
{
    public async Task<Result<IReadOnlyList<RoleDto>>> Handle(ListRolesQuery request, CancellationToken ct)
    {
        var roles = await roleRepository.GetAllActiveAsync(ct);

        var dtos = roles
            .Select(r => new RoleDto(r.Id, r.Name, r.Description, r.IsActive))
            .ToList();

        return Result<IReadOnlyList<RoleDto>>.Success(dtos);
    }
}
