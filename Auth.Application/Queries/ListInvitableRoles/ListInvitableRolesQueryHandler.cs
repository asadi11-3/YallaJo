using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Queries.ListInvitableRoles;

public sealed class ListInvitableRolesQueryHandler(
    IUserRegistrationService userRegistrationService)
    : IQueryHandler<ListInvitableRolesQuery, IReadOnlyList<InvitableRoleOptionDto>>
{
    public async Task<Result<IReadOnlyList<InvitableRoleOptionDto>>> Handle(
        ListInvitableRolesQuery request,
        CancellationToken cancellationToken)
    {
        var result = await userRegistrationService.ListInvitableRolesAsync(cancellationToken);

        return result.Map(roles => (IReadOnlyList<InvitableRoleOptionDto>)roles
            .Select(r => new InvitableRoleOptionDto(
                r.RoleId,
                r.Name,
                r.Description,
                r.IsPrivileged))
            .ToList());
    }
}
