namespace Security.Contracts.Abstractions;

public sealed record InvitableRoleOption(
    Guid RoleId,
    string Name,
    string? Description,
    bool IsPrivileged);
