namespace YallaJo.Web.Areas.Admin.Models.Invitations;

public sealed class InvitableRoleOptionResponse
{
    public Guid RoleId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsPrivileged { get; init; }
}
