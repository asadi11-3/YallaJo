namespace YallaJo.Web.Areas.Admin.Modules.Accounts.Features.Invitations.ViewModels;

public sealed class InvitableRoleOptionVm
{
    public Guid RoleId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsPrivileged { get; init; }
}
