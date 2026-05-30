namespace Auth.Application.Queries.ListInvitableRoles;

public sealed record InvitableRoleOptionDto(
    Guid RoleId,
    string Name,
    string? Description,
    bool IsPrivileged);
