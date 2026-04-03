using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Queries.ListRoles;

public sealed record ListRolesQuery : IQuery<IReadOnlyList<RoleDto>>;
