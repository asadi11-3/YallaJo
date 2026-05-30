namespace Auth.Presentation.Endpoints.Invitation.Models;

public sealed record InvitableRoleOptionResponse(
    Guid RoleId,
    string Name,
    string? Description,
    bool IsPrivileged);
