
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Queries.ListRoles;

public sealed record RoleDto(Guid Id, string Name, string? Description, bool IsActive);

public sealed record ListRolesQuery : IQuery<IReadOnlyList<RoleDto>>;
