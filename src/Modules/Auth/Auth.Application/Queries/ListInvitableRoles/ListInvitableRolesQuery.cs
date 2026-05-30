using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Queries.ListInvitableRoles;

public sealed record ListInvitableRolesQuery : IQuery<IReadOnlyList<InvitableRoleOptionDto>>;
